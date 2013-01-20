using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Dema.BlenX.VisualStudio
{
   /// <summary>
   /// Class used to identify a module. The module is identify using the hierarchy that
   /// contains it and its item id inside the hierarchy.
   /// </summary>
   internal sealed class ModuleId
   {
      private IVsHierarchy ownerHierarchy;
      private uint itemId;
      public ModuleId(IVsHierarchy owner, uint id)
      {
         this.ownerHierarchy = owner;
         this.itemId = id;
      }
      public IVsHierarchy Hierarchy
      {
         get { return ownerHierarchy; }
      }
      public uint ItemID
      {
         get { return itemId; }
      }
      public override int GetHashCode()
      {
         int hash = 0;
         if (null != ownerHierarchy)
         {
            hash = ownerHierarchy.GetHashCode();
         }
         hash = hash ^ (int)itemId;
         return hash;
      }
      public override bool Equals(object obj)
      {
         ModuleId other = obj as ModuleId;
         if (null == obj)
         {
            return false;
         }
         if (!ownerHierarchy.Equals(other.ownerHierarchy))
         {
            return false;
         }
         return (itemId == other.itemId);
      }
   }

   /// <summary>
   /// This interface defines the service that finds IronPython files inside a hierarchy
   /// and builds the informations to expose to the class view or object browser.
   /// </summary>
   [Guid(BlenXPackageConstants.libraryManagerServiceGuidString)]
   public interface IBlenXLibraryManager
   {
      void RegisterHierarchy(IVsHierarchy hierarchy);
      void UnregisterHierarchy(IVsHierarchy hierarchy);
      //void RegisterLineChangeHandler(uint document, TextLineChangeEvent lineChanged, Action<IVsTextLines> onIdle);
   }

   /// <summary>
   /// Inplementation of the service that build the information to expose to the symbols
   /// navigation tools (class view or object browser) from the Python files inside a
   /// hierarchy.
   /// </summary>
   [Guid(BlenXPackageConstants.libraryManagerGuidString)]
   internal class BlenXLibraryManager : IBlenXLibraryManager, IVsRunningDocTableEvents, IDisposable
   {
      /// <summary>
      /// Class storing the data about a parsing task on a python module.
      /// A module in IronPython is a source file, so here we use the file name to
      /// identify it.
      /// </summary>
      private class LibraryTask
      {

         private string fileName;
         private string text;
         private ModuleId moduleId;

         public LibraryTask(string fileName, string text)
         {
            this.fileName = fileName;
            this.text = text;
         }

         public string FileName
         {
            get { return fileName; }
         }
         public ModuleId ModuleID
         {
            get { return moduleId; }
            set { moduleId = value; }
         }
         public string Text
         {
            get { return text; }
         }
      }

      private Thread parseThread;
      private ManualResetEvent requestPresent;
      private ManualResetEvent shutDownStarted;
      private Queue<LibraryTask> requests;
      private Babel.Parser.ErrorHandler handler = null;


      private IServiceProvider provider;
      private Dictionary<IVsHierarchy, HierarchyListener> hierarchies = new Dictionary<IVsHierarchy, HierarchyListener>();
      //private uint objectManagerCookie;

      private static SymbolTable symbolTable = new SymbolTable();
      public static SymbolTable SymbolTable { get { return symbolTable; } }

      public BlenXLibraryManager(IServiceProvider provider)
      {
         //documents = new Dictionary<uint, TextLineEventListener>();
         //library = new Library(new Guid("0925166e-a743-49e2-9224-bbe206545104"));
         //library.LibraryCapabilities = (_LIB_FLAGS2)_LIB_FLAGS.LF_PROJECT;
         //files = new Dictionary<ModuleId, LibraryNode>();
         this.provider = provider;
         requests = new Queue<LibraryTask>();
         requestPresent = new ManualResetEvent(false);
         shutDownStarted = new ManualResetEvent(false);
         parseThread = new Thread(new ThreadStart(ParseThread));
         parseThread.Start();

         this.handler = new Babel.Parser.ErrorHandler(provider);
      }

      public void RegisterHierarchy(IVsHierarchy hierarchy)
      {
         // Refresh symbol table
         symbolTable = new SymbolTable();

         if ((null == hierarchy) || hierarchies.ContainsKey(hierarchy))
         {
            return;
         }
         //if (0 == objectManagerCookie)
         //{
         //    IVsObjectManager2 objManager = provider.GetService(typeof(SVsObjectManager)) as IVsObjectManager2;
         //    if (null == objManager)
         //    {
         //        return;
         //    }
         //    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
         //        objManager.RegisterSimpleLibrary(library, out objectManagerCookie));
         //}
         HierarchyListener listener = new HierarchyListener(hierarchy);
         listener.OnAddItem += new EventHandler<HierarchyEventArgs>(OnNewFile);
         listener.OnDeleteItem += new EventHandler<HierarchyEventArgs>(OnDeleteFile);
         listener.StartListening(true);
         hierarchies.Add(hierarchy, listener);
         //RegisterForRDTEvents();
      }

      public void UnregisterHierarchy(IVsHierarchy hierarchy)
      {
         if ((null == hierarchy) || !hierarchies.ContainsKey(hierarchy))
         {
            return;
         }
         HierarchyListener listener = hierarchies[hierarchy];
         if (null != listener)
         {
            listener.Dispose();
         }
         hierarchies.Remove(hierarchy);
         //if (0 == hierarchies.Count)
         //{
         //    UnregisterRDTEvents();
         //}
         //lock (files)
         //{
         //    ModuleId[] keys = new ModuleId[files.Keys.Count];
         //    files.Keys.CopyTo(keys, 0);
         //    foreach (ModuleId id in keys)
         //    {
         //        if (hierarchy.Equals(id.Hierarchy))
         //        {
         //            library.RemoveNode(files[id]);
         //            files.Remove(id);
         //        }
         //    }
         //}
         // Remove the document listeners.
         //uint[] docKeys = new uint[documents.Keys.Count];
         //documents.Keys.CopyTo(docKeys, 0);
         //foreach (uint id in docKeys)
         //{
         //    TextLineEventListener docListener = documents[id];
         //    if (hierarchy.Equals(docListener.FileID.Hierarchy))
         //    {
         //        documents.Remove(id);
         //        docListener.Dispose();
         //    }
         //}
      }

      private void OnNewFile(object sender, HierarchyEventArgs args)
      {
         IVsHierarchy hierarchy = sender as IVsHierarchy;
         if (null == hierarchy)
         {
            return;
         }
         string fileText = null;
         if (null != args.TextBuffer)
         {
            int lastLine;
            int lastIndex;
            int hr = args.TextBuffer.GetLastLineIndex(out lastLine, out lastIndex);
            if (Microsoft.VisualStudio.ErrorHandler.Failed(hr))
            {
               return;
            }
            hr = args.TextBuffer.GetLineText(0, 0, lastLine, lastIndex, out fileText);
            if (Microsoft.VisualStudio.ErrorHandler.Failed(hr))
            {
               return;
            }
         }
         CreateParseRequest(args.CanonicalName, fileText, new ModuleId(hierarchy, args.ItemID));
      }

      #region Parse Thread
      /// <summary>
      /// Main function of the parsing thread.
      /// This function waits on the queue of the parsing requests and build the parsing tree for
      /// a specific file. The resulting tree is built using LibraryNode objects so that it can
      /// be used inside the class view or object browser.
      /// </summary>
      private void ParseThread()
      {
         const int waitTimeout = 500;
         // Define the array of events this function is interest in.
         WaitHandle[] eventsToWait = new WaitHandle[] { requestPresent, shutDownStarted };
         // Execute the tasks.
         while (true)
         {
            // Wait for a task or a shutdown request.
            int waitResult = WaitHandle.WaitAny(eventsToWait, waitTimeout, false);
            if (1 == waitResult)
            {
               // The shutdown of this component is started, so exit the thread.
               return;
            }
            LibraryTask task = null;
            lock (requests)
            {
               if (0 != requests.Count)
               {
                  task = requests.Dequeue();
               }
               if (0 == requests.Count)
               {
                  requestPresent.Reset();
               }
            }
            if (task == null)
            {
               continue;
            }

            if (task.Text == null)
            {
               if (System.IO.File.Exists(task.FileName))
               {
                  var parser = new Babel.Parser.Parser();
                  handler.Clear();

                  using (var stream = File.OpenRead(task.FileName))
                  {
                     Babel.Lexer.Scanner scanner = new Babel.Lexer.Scanner(stream);

                     handler.SetFileName(task.FileName);

                     scanner.Handler = handler;
                     Debug.Assert(symbolTable != null);
                     parser.SetParsingInfo(task.FileName, symbolTable, handler);
                     parser.scanner = scanner;

                     //parser.MBWInit(req);
                     var retval = parser.Parse();
                     // the parse result is to fill the symbol table

                  }
               }
            }
            else
            {
               var parser = new Babel.Parser.Parser();
               handler.Clear();

               Babel.Lexer.Scanner scanner = new Babel.Lexer.Scanner(); // string interface

               handler.SetFileName(task.FileName);

               scanner.Handler = handler;
               Debug.Assert(symbolTable != null);
               parser.SetParsingInfo(task.Text, symbolTable, handler);
               parser.scanner = scanner;

               scanner.SetSource(task.Text, 0);

               //parser.MBWInit(req);
               parser.Parse();
            }
            //LibraryNode module = new LibraryNode(
            //        System.IO.Path.GetFileName(task.FileName),
            //        LibraryNode.LibraryNodeType.PhysicalContainer);
            //CreateModuleTree(module, module, scope, "", task.ModuleID);
            //if (null != task.ModuleID)
            //{
            //    LibraryNode previousItem = null;
            //    lock (files)
            //    {
            //        if (files.TryGetValue(task.ModuleID, out previousItem))
            //        {
            //            files.Remove(task.ModuleID);
            //        }
            //    }
            //    library.RemoveNode(previousItem);
            //}
            //library.AddNode(module);
            //if (null != task.ModuleID)
            //{
            //    lock (files)
            //    {
            //        files.Add(task.ModuleID, module);
            //    }
            //}
         }
      }

      //private void CreateModuleTree(LibraryNode root, LibraryNode current, ScopeNode scope, string namePrefix, ModuleId moduleId)
      //{
      //    if ((null == root) || (null == scope) || (null == scope.NestedScopes))
      //    {
      //        return;
      //    }
      //    foreach (ScopeNode subItem in scope.NestedScopes)
      //    {
      //        PythonLibraryNode newNode = new PythonLibraryNode(subItem, namePrefix, moduleId.Hierarchy, moduleId.ItemID);
      //        string newNamePrefix = namePrefix;

      //        // The classes are always added to the root node, the functions to the
      //        // current node.
      //        if ((newNode.NodeType & LibraryNode.LibraryNodeType.Members) != LibraryNode.LibraryNodeType.None)
      //        {
      //            current.AddNode(newNode);
      //        }
      //        else if ((newNode.NodeType & LibraryNode.LibraryNodeType.Classes) != LibraryNode.LibraryNodeType.None)
      //        {
      //            // Classes are always added to the root.
      //            root.AddNode(newNode);
      //            newNamePrefix = newNode.Name + ".";
      //        }

      //        // Now use recursion to get the other types.
      //        CreateModuleTree(root, newNode, subItem, newNamePrefix, moduleId);
      //    }
      //}
      #endregion

      private void CreateParseRequest(string file, string text, ModuleId id)
      {
         LibraryTask task = new LibraryTask(file, text);
         task.ModuleID = id;
         lock (requests)
         {
            requests.Enqueue(task);
         }
         requestPresent.Set();
      }

      private void OnDeleteFile(object sender, HierarchyEventArgs args)
      {
         IVsHierarchy hierarchy = sender as IVsHierarchy;
         if (null == hierarchy)
         {
            return;
         }
         symbolTable.RemoveFile(args.CanonicalName);

         //ModuleId id = new ModuleId(hierarchy, args.ItemID);
         //LibraryNode node = null;
         //lock (files)
         //{
         //    if (files.TryGetValue(id, out node))
         //    {
         //        files.Remove(id);
         //    }
         //}
         //if (null != node)
         //{
         //    library.RemoveNode(node);
         //}
      }

      public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
      {
         throw new NotImplementedException();
      }

      public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
      {
         throw new NotImplementedException();
      }

      public int OnAfterSave(uint docCookie)
      {
         throw new NotImplementedException();
      }

      public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
      {
         throw new NotImplementedException();
      }

      public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
      {
         throw new NotImplementedException();
      }

      public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
      {
         throw new NotImplementedException();
      }

      public void Dispose()
      {
         // Make sure that the parse thread can exit.
         if (shutDownStarted != null)
            shutDownStarted.Set();

         if ((null != parseThread) && parseThread.IsAlive)
         {
            parseThread.Join(500);
            if (parseThread.IsAlive)
               parseThread.Abort();

            parseThread = null;
         }

         requests.Clear();

         // Dispose all the listeners.
         foreach (HierarchyListener listener in hierarchies.Values)
            listener.Dispose();

         hierarchies.Clear();
      }
   }
}
