/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.BuildEngine;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace Dema.VisualStudio
{  

    class BuildManager : IBuildManager
    {
        //public const string RunWithBuildFlag = "RunCodeSweepAfterBuild";

        /// <summary>
        /// Creates a new build manager object.
        /// </summary>
        /// <param name="provider">The service provider that will be used to get VS services.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>serviceProvider</c> is null.</exception>
        public BuildManager(IServiceProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            _serviceProvider = provider;
        }

        #region IBuildManager Members

        public event EmptyEvent BuildStarted;
        public event EmptyEvent BuildStopped;

        /// <summary>
        /// Gets or sets whether this object is subscribed to the build events fired by the VS IDE.
        /// </summary>
        /// <remarks>
        /// If this object is subscribed to build events, it will clear the CodeSweep task list and
        /// set the host object for all ScannerTask tasks in all projects when the build begins.
        /// </remarks>
        public bool IsListeningToBuildEvents
        {
            get
            {
                return _listening;
            }
            set
            {
                if (value != _listening)
                {
                    if (value)
                    {
                        GetBuildEvents().OnBuildBegin += new EnvDTE._dispBuildEvents_OnBuildBeginEventHandler(buildEvents_OnBuildBegin);
                        GetBuildEvents().OnBuildDone += new EnvDTE._dispBuildEvents_OnBuildDoneEventHandler(buildEvents_OnBuildDone);
                    }
                    else
                    {
                        GetBuildEvents().OnBuildBegin -= new EnvDTE._dispBuildEvents_OnBuildBeginEventHandler(buildEvents_OnBuildBegin);
                        GetBuildEvents().OnBuildDone -= new EnvDTE._dispBuildEvents_OnBuildDoneEventHandler(buildEvents_OnBuildDone);
                    }
                    _listening = value;
                }
            }
        }

        /// <summary>
        /// Gets the CodeSweep build task for the given project, optionally creating it if it does not exist.
        /// </summary>
        /// <param name="project">The project from which the task will be retrieved.</param>
        /// <param name="createIfNecessary">If true, the task will be created if it does not exist.</param>
        /// <returns>The task object retrieved, or null if no task was found and <c>createIfNecessary</c> is false.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>project</c> is null.</exception>
        /// <remarks>
        /// If the task is created, a <c>UsingTask</c> entry is also inserted into the project to
        /// specify the location of the dll.
        /// The task is created in the "AfterBuild" target of the project file.
        /// The task is created with the following properties:
        ///     Condition = "'$(RunCodeSweepAfterBuild)' == 'true'"
        ///     ContinueOnError = false
        /// and the following parameters:
        ///     FilesToScan = all item groups in project except "Reference", formatted as "@(group1);@(group2);..."
        ///     TermTables = [user's app data folder]\CodeSweep\sample_term_table.xml
        ///     Project = "$(MSBuildProjectFullPath)"
        /// </remarks>
        public Microsoft.Build.BuildEngine.BuildTask GetBuildTask(IVsProject project, bool createIfNecessary)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            if (createIfNecessary)
            {
                CreateTaskIfNecessary(project);
            }

            Project msbuildProject = MSBuildProjectFromIVsProject(project);
            return GetNamedTask(msbuildProject.Targets["AfterBuild"], "ScannerTask");
        }

        /// <summary>
        /// Transforms a relative path to an absolute one based on a specified base folder.
        /// </summary>
        static public string AbsolutePathFromRelative(string relativePath, string baseFolderForDerelativization)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException("relativePath");
            }
            if (baseFolderForDerelativization == null)
            {
                throw new ArgumentNullException("baseFolderForDerelativization");
            }
            if (Path.IsPathRooted(relativePath))
            {
                throw new ArgumentException("relativePath");
            }
            if (!Path.IsPathRooted(baseFolderForDerelativization))
            {
                throw new ArgumentException("baseFolderForDerelativization");
            }

            StringBuilder result = new StringBuilder(baseFolderForDerelativization);

            if (result[result.Length - 1] != Path.DirectorySeparatorChar)
            {
                result.Append(Path.DirectorySeparatorChar);
            }

            int spanStart = 0;

            while (spanStart < relativePath.Length)
            {
                int spanStop = relativePath.IndexOf(Path.DirectorySeparatorChar, spanStart);

                if (spanStop == -1)
                {
                    spanStop = relativePath.Length;
                }

                string span = relativePath.Substring(spanStart, spanStop - spanStart);

                if (span == "..")
                {
                    // The result string should end with a directory separator at this point.  We
                    // want to search for the one previous to that, which is why we subtract 2.
                    int previousSeparator = result.ToString().LastIndexOf(Path.DirectorySeparatorChar, result.Length - 2);
                    if (previousSeparator == -1)
                    {
                        throw new ArgumentException();
                    }
                    result.Remove(previousSeparator, result.Length - previousSeparator);
                }
                else if (span != ".")
                {
                    result.Append(span);
                }

                if (spanStop < relativePath.Length)
                {
                    result.Append(Path.DirectorySeparatorChar);
                }

                spanStart = spanStop + 1;
            }

            return result.ToString();
        }


        /// <summary>
        /// Enumerates all items in the project except those in the "Reference" group.
        /// </summary>
        /// <param name="project">The project from which to retrieve the items.</param>
        /// <returns>A list of item "Include" values.  For items that specify files, these will be the file names.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>project</c> is null.</exception>
        public ICollection<string> AllItemsInProject(IVsProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            string projectDir = Path.GetDirectoryName(ProjectUtilities.GetProjectFilePath(project));
            IVsHierarchy hierarchy = project as IVsHierarchy;

            List<string> allNames = ChildrenOf(hierarchy, HierarchyConstants.VSITEMID_ROOT).ConvertAll<string>(
                delegate(uint id)
                {
                    string name = null;
                    project.GetMkDocument(id, out name);
                    if (name != null && name.Length > 0 && !Path.IsPathRooted(name))
                    {
                        name = AbsolutePathFromRelative(name, projectDir);
                    }
                    return name;
                });

            allNames.RemoveAll(
                delegate(string name)
                {
                    return !File.Exists(name);
                });

            return allNames;
        }

        /// <summary>
        /// Sets a property in the given project, overriding the existing value if it exists.
        /// </summary>
        /// <param name="project">The project in which the property will be set.</param>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>project</c>, <c>name</c>, or <c>value</c> is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if <c>name</c> is empty or contains invalid characters.</exception>
        public void SetProperty(IVsProject project, string name, string value)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (name.Length == 0)
            {
                throw new ArgumentException("name");
            }

            Project msbuildProject = MSBuildProjectFromIVsProject(project);

            BuildProperty property = FindProperty(msbuildProject, name);
            if (property == null)
            {
                BuildPropertyGroup group = msbuildProject.AddNewPropertyGroup(true);
                group.AddNewProperty(name, value);
            }
            else
            {
                property.Value = value;
            }
        }

        /// <summary>
        /// Gets a property from the given project.
        /// </summary>
        /// <param name="project">The project from which the property will be retrieved.</param>
        /// <param name="name">The name of the property.</param>
        /// <returns>The value of the specified property, or null if it does not exist.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <c>project</c> or <c>name</c> is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if <c>name</c> is empty.</exception>
        public string GetProperty(IVsProject project, string name)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (name.Length == 0)
            {
                throw new ArgumentException("name");
            }

            Project msbuildProject = MSBuildProjectFromIVsProject(project);
            BuildProperty property = FindProperty(msbuildProject, name);

            if (property == null)
            {
                return null;
            }
            else
            {
                return property.Value;
            }
        }

        /// <summary>
        /// Creates the per-user files for the current user if they have not yet been created.
        /// </summary>
        public void CreatePerUserFilesAsNecessary()
        {
            //TODO!!!
        }

        #endregion

        #region Private Members

        IServiceProvider _serviceProvider;
        bool _listening = false;
        EnvDTE.BuildEvents _buildEvents;

        
        private void SetHostObject()
        {
            foreach (string projectName in ProjectNames)
            {
                if (projectName != null)
                {
                    Project msbuildProject = Engine.GlobalEngine.GetLoadedProject(projectName);

                    if (msbuildProject != null)
                    {
                        Target target = msbuildProject.Targets["AfterBuild"];

                        if (target != null)
                        {
                            foreach (object taskObj in msbuildProject.Targets["AfterBuild"])
                            {
                                Microsoft.Build.BuildEngine.BuildTask task = (Microsoft.Build.BuildEngine.BuildTask)taskObj;
                                if (task.Name == "ScannerTask")
                                {
                                    task.HostObject = Factory.GetScannerHost();
                                }
                            }
                        }
                    }
                }
            }
        }

        private static Project MSBuildProjectFromIVsProject(IVsProject project)
        {
            return Engine.GlobalEngine.GetLoadedProject(ProjectUtilities.GetProjectFilePath(project));
        }

        private static Microsoft.Build.BuildEngine.BuildTask GetNamedTask(Target target, string taskName)
        {
            if (target != null)
            {
                foreach (object taskObj in target)
                {
                    Microsoft.Build.BuildEngine.BuildTask task = (Microsoft.Build.BuildEngine.BuildTask)taskObj;
                    if (task.Name == taskName)
                    {
                        return task;
                    }
                }
            }

            return null;
        }

        private static void CreateTaskIfNecessary(IVsProject project)
        {
            Project msbuildProject = MSBuildProjectFromIVsProject(project);
            Target target = msbuildProject.Targets["AfterBuild"];
            if (target == null || target.IsImported)
            {
                target = msbuildProject.Targets.AddNewTarget("AfterBuild");
            }

            string importFolder = Path.GetDirectoryName(typeof(CodeSweep.BuildTask.ScannerTask).Module.FullyQualifiedName);
            string importPath = Utilities.EncodeProgramFilesVar(importFolder) + "\\CodeSweep.targets";
            string installedCondition = "Exists('" + importPath + "')";

            if (GetNamedTask(target, "ScannerTask") == null)
            {
                string projectFolder = Path.GetDirectoryName(ProjectUtilities.GetProjectFilePath(project));
                Microsoft.Build.BuildEngine.BuildTask newTask = target.AddNewTask("ScannerTask");
                newTask.Condition = installedCondition + " and '$(" + RunWithBuildFlag + ")' == 'true'";
                newTask.ContinueOnError = false;
                newTask.SetParameterValue("FilesToScan", CodeSweep.Utilities.Concatenate(AllItemGroupsInProject(msbuildProject), ";"));
                newTask.SetParameterValue("TermTables", Globals.DefaultTermTablePath);
                newTask.SetParameterValue("Project", "$(MSBuildProjectFullPath)");
            }

            bool found = false;

            foreach (object importObj in msbuildProject.Imports)
            {
                Import import = (Import)importObj;

                if (import.ProjectPath == importPath)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                msbuildProject.AddNewImport(importPath, installedCondition);

                // This is a workaround for bug in MSBuild that was found too late to be fixed
                // for VS 2005.  Project.Imports does not get updated with the newly added import
                // unless you call Project.EvaluatedProperties.
                BuildPropertyGroup dummy = msbuildProject.EvaluatedProperties;
            }
        }

        private static BuildProperty FindProperty(Project msbuildProject, string name)
        {
            foreach (object groupObj in msbuildProject.PropertyGroups)
            {
                BuildPropertyGroup group = (BuildPropertyGroup)groupObj;

                foreach (object propertyObj in group)
                {
                    BuildProperty property = (BuildProperty)propertyObj;

                    if (property.Name == name)
                    {
                        return property;
                    }
                }
            }
            return null;
        }

        private static IEnumerable<string> AllItemGroupsInProject(Project project)
        {
            List<string> names = new List<string>();

            foreach (BuildItemGroup group in project.ItemGroups)
            {
                foreach (object itemObj in group)
                {
                    BuildItem item = (BuildItem)itemObj;
                    if (!item.IsImported && item.Name != "Reference")
                    {
                        string groupName = "@(" + item.Name + ")";
                        if (!names.Contains(groupName))
                        {
                            names.Add(groupName);
                        }
                    }
                }
            }

            return names;
        }

        private void buildEvents_OnBuildBegin(EnvDTE.vsBuildScope Scope, EnvDTE.vsBuildAction Action)
        {
            if (Action == EnvDTE.vsBuildAction.vsBuildActionBuild || Action == EnvDTE.vsBuildAction.vsBuildActionRebuildAll)
            {
                Factory.GetBackgroundScanner().StopIfRunning(true);
                Factory.GetTaskProvider().Clear();
                Factory.GetTaskProvider().SetAsActiveProvider();
                SetHostObject();

                if (BuildStarted != null)
                {
                    BuildStarted();
                }
            }
        }

        void buildEvents_OnBuildDone(EnvDTE.vsBuildScope Scope, EnvDTE.vsBuildAction Action)
        {
            if (BuildStopped != null)
            {
                BuildStopped();
            }
        }

        private IEnumerable<string> ProjectNames
        {
            get
            {
                IVsSolution solution = _serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;

                uint projectCount = 0;
                int hr = solution.GetProjectFilesInSolution((uint)__VSGETPROJFILESFLAGS.GPFF_SKIPUNLOADEDPROJECTS, 0, null, out projectCount);
                System.Diagnostics.Debug.Assert(hr == VSConstants.S_OK, "GetProjectFilesInSolution failed.");

                string[] projectNames = new string[projectCount];
                hr = solution.GetProjectFilesInSolution((uint)__VSGETPROJFILESFLAGS.GPFF_SKIPUNLOADEDPROJECTS, projectCount, projectNames, out projectCount);
                System.Diagnostics.Debug.Assert(hr == VSConstants.S_OK, "GetProjectFilesInSolution failed.");

                return projectNames;
            }
        }

        private EnvDTE.BuildEvents GetBuildEvents()
        {
            if (_buildEvents == null)
            {
                EnvDTE.DTE dte = _serviceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;

                _buildEvents = dte.Events.BuildEvents;
            }
            return _buildEvents;
        }

        private List<uint> ChildrenOf(IVsHierarchy hierarchy, uint rootID)
        {
            List<uint> result = new List<uint>();

            for (uint itemID = FirstChild(hierarchy, rootID); itemID != HierarchyConstants.VSITEMID_NIL; itemID = NextSibling(hierarchy, itemID))
            {
                result.Add(itemID);
                result.AddRange(ChildrenOf(hierarchy, itemID));
            }

            return result;
        }

        private static uint FirstChild(IVsHierarchy hierarchy, uint rootID)
        {
            object childIDObj = null;
            hierarchy.GetProperty(rootID, (int)__VSHPROPID.VSHPROPID_FirstChild, out childIDObj);
            if (childIDObj != null)
            {
                return (uint)(int)childIDObj;
            }

            return HierarchyConstants.VSITEMID_NIL;
        }

        private static uint NextSibling(IVsHierarchy hierarchy, uint firstID)
        {
            object siblingIDObj = null;
            hierarchy.GetProperty(firstID, (int)__VSHPROPID.VSHPROPID_NextSibling, out siblingIDObj);
            if (siblingIDObj != null)
            {
                return (uint)(int)siblingIDObj;
            }

            return HierarchyConstants.VSITEMID_NIL;
        }

        #endregion
    }
}
