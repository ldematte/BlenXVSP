using System;
using System.Collections.Generic;
using System.Text;
using Wintellect.PowerCollections;
using System.Diagnostics;

namespace Dema.BlenX.Parser
{

   public interface INodeVisitor
   {
      bool Visit(Node node);
      bool Visit(Declaration declNode);
      bool Visit(PTN_Box betaNode);
      bool Visit(PiProcess pprocNode);
      bool Visit(Molecule molNode);
      bool Visit(BinderNode binderNode);
      bool Visit(Reaction reaction);
   }

   //not an interface, so implementors can decide what to subclass
   public abstract class NodeVisitor : INodeVisitor
   {
      public virtual bool Visit(Node node) { return true; }
      public virtual bool Visit(Declaration declNode) { return true; }
      public virtual bool Visit(PTN_Box betaNode) { return true; }
      public virtual bool Visit(PiProcess pprocNode) { return true; }
      public virtual bool Visit(Molecule molNode) { return true; }
      public virtual bool Visit(BinderNode binderNode) { return true; }
      public virtual bool Visit(Reaction reaction) { return true; }
   }


   public abstract class Node
   {
      public static System.IO.TextWriter Out = null;

      protected List<Node> childNodes = new List<Node>();
      protected System.IO.TextWriter myOut;

      protected Pos pos;

      public IList<Node> Children { get { return childNodes; } }
      //TODO: replace with getter
      public List<Node> GetChildren()
      {
         return childNodes;
      }

      public Pos Position
      {
         get { return pos; }
      }

      protected Node(Pos pos)
      {
         this.pos = pos;
         if (Node.Out != null)
            myOut = Node.Out;
         else
            myOut = Console.Out;
      }

      public abstract void Accept(INodeVisitor visitor);

      public virtual void Print(int level)
      {
         for (int i = 0; i < level; ++i)
            myOut.Write("  ");

         System.Type type = this.GetType();
         myOut.WriteLine(type.FullName);

         foreach (Node childNode in childNodes)
            childNode.Print(level + 1);
      }

      public virtual void Print()
      {
         Print(0);
      }

      public abstract void GenerateText(StringBuilder sb);

      public abstract Node Clone();
   }

   // This AST node is part of a list
   // In BNF is something like list :: elem | elem list;
   // T is the type of the node that will be inserted in the List
   // for example:
   // NUMLIST :: NUM | NUM NUMLIST
   // the actual class is NUM: Node, but the resulting type in list
   // should be int, so NUM: NodeListElem<int>
   public abstract class NodeListElem<T> : Node
   {
      public NodeListElem(Pos pos) : base(pos) { }
      public abstract void BuildList(ICollection<T> list);
   }

   // Another possinbility, useful for declarations
   // BNF is still a list (elem | elem list), but each elem
   // has an identifier and a type
   public abstract class NodeListElemWithID<T> : NodeListElem<T>
   {
      protected string id;
      protected T myElem;

      public NodeListElemWithID(Pos pos) : base(pos) { }

      public string ID
      {
         get { return id; }
      }

      public virtual void BuildNamedList(IDictionary<string, T> namedCollection)
      {
         namedCollection.Add(id, myElem);
      }

      public override Node Clone()
      {
         throw new NotImplementedException();
      }
   }


   public abstract class NodeList<U, T> : Node
      where U : NodeListElem<T>
   {
      public NodeList(Pos pos)
         : base(pos)
      {
      }

      public void BuildList(ICollection<T> list)
      {
         ((U)childNodes[0]).BuildList(list);
         if (childNodes.Count > 1)
            ((NodeList<U, T>)childNodes[1]).BuildList(list);
      }
   }

   public abstract class NodeListWithID<U, T> : NodeList<U, T>
      where U : NodeListElemWithID<T>
   {
      public NodeListWithID(Pos pos)
         : base(pos)
      {
      }

      // TODO: IEnumerator!!
      public virtual void BuildNamedList(IDictionary<string, T> namedColl)
      {
         ((U)childNodes[0]).BuildNamedList(namedColl);
         if (childNodes.Count > 1)
            ((NodeListWithID<U, T>)childNodes[1]).BuildNamedList(namedColl);
      }

      public override Node Clone()
      {
         throw new NotImplementedException();
      }
   }


   public enum NodeType
   {
      PROGRAM,
      INFO,
      RATE,
      RATE_PAIR,
      DEC,
      DEC_PAIR,
      NAME_LIST,
      NAME_LIST_PAIR,

      NIL,
      ID,
      ACTION,
      REPLICATION,
      CHOICE,
      PARALLEL,

      IO,
      EXPOSE,
      EXPOSE_NL,
      HU,

      BETA,
      BINDER,
      BINDER_PAIR,
      BINDER_NL,
      BP,
      BP_PAIR
   }

   public enum InfoType
   {
      STEPS,
      STEPS_DELTA,
      TIME
   }

   public class Info : Node
   {
      InfoType infoType;

      public int Steps = 1;
      public double Time = Double.PositiveInfinity;
      public double Delta = 0.0;

      public Info(Pos pos, InfoType infoType, int steps)
         : base(pos)
      {
         this.infoType = infoType;
         Steps = steps;
      }

      public Info(Pos pos, InfoType infoType, int steps, double delta)
         : base(pos)
      {
         this.infoType = infoType;
         Steps = steps;
         Delta = delta;
      }

      public Info(Pos pos, InfoType infoType, double time)
         : base(pos)
      {
         this.infoType = infoType;
         Time = time;
      }

      public override void GenerateText(StringBuilder txt)
      {
         if (Steps > 1)
         {
            txt.Append("[steps = " + Steps.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (Delta != 0.0)
               txt.Append(", delta = " + Delta.ToString(System.Globalization.CultureInfo.InvariantCulture));
            txt.AppendLine("]");
         }
         else
         {
            txt.AppendLine("[time = " + Time.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]");
         }
         txt.AppendLine();
      }

      public override Node Clone()
      {
         switch (infoType)
         {
            case InfoType.STEPS:
               return new Info(Position, infoType, this.Steps);

            case InfoType.STEPS_DELTA:
               return new Info(Position, infoType, this.Steps, this.Delta);

            default:
               return new Info(Position, infoType, this.Time);
         }
      }

      public override void  Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }
   }

   public class HypParam : NodeListElem<Pair<string, string>>
   {
      private string paramA;
      private string paramB;

      public HypParam(Pos pos, string paramA, string paramB)
         : base(pos)
      {
         this.paramA = paramA;
         this.paramB = paramB;
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void BuildList(ICollection<Pair<string, string>> list)
      {
         list.Add(new Pair<string, string>(paramA, paramB));
      }

      public override void GenerateText(StringBuilder sb)
      {
         sb.Append("(");
         sb.Append(paramA);
         sb.Append(", ");
         sb.Append(paramB);
         sb.Append(")");
      }

      public override Node Clone()
      {
         return new HypParam(this.Position, this.paramA, this.paramB);
      }
   }

   public class PTN_HypExp_Parameter : NodeList<HypParam, Pair<string, string>>
   {
      public PTN_HypExp_Parameter(Pos pos, Node param, Node otherParams)
         : base(pos)
      {
         childNodes.Add(param);
         childNodes.Add(otherParams);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public PTN_HypExp_Parameter(Pos pos, Node param)
         : base(pos)
      {
         childNodes.Add(param);
      }

      public override void GenerateText(StringBuilder sb)
      {
         childNodes[0].GenerateText(sb);
         if (childNodes.Count > 1)
         {
            sb.Append(", ");
            childNodes[1].GenerateText(sb);
         }
      }

      public override Node Clone()
      {
         throw new NotImplementedException();
      }
   }

   public class Program : Node
   {
      public Program(Pos pos, Node info, Node rateList, Node decList, Node bpList)
         : base(pos)
      {
         childNodes.Add(info);
         childNodes.Add(rateList);
         childNodes.Add(decList);
         childNodes.Add(bpList);
         ((BpList)bpList).BuildList(RunList);
         ((RateList)rateList).BuildList(Rates);
         ((DeclarationList)decList).BuildNamedList(DecList);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public Program(Pos pos, Node info, Node decList, Node bpList)
         : base(pos)
      {
         childNodes.Add(info);
         childNodes.Add(decList);
         childNodes.Add(bpList);
         ((BpList)bpList).BuildList(RunList);
         ((DeclarationList)decList).BuildNamedList(DecList);
      }

      public Program(Pos pos, Node decList, Node bpList)
         : base(pos)
      {
         childNodes.Add(decList);
         childNodes.Add(bpList);
         ((BpList)bpList).BuildList(RunList);
         ((DeclarationList)decList).BuildNamedList(DecList);
      }

      public Program(Pos pos, Node info, List<Pair<string, int>> newRunList, List<Rate> newRates, Dictionary<string, Declaration> newDecList)
         : base(pos)
      {
         childNodes.Add(info);
         RunList = newRunList;
         Rates = newRates;
         DecList = newDecList;
      }

      public List<Pair<string, int>> RunList = new List<Pair<string, int>>();
      public List<Rate> Rates = new List<Rate>();
      public Dictionary<string, Declaration> DecList = new Dictionary<string, Declaration>();

      public override void GenerateText(StringBuilder sb)
      {
         // header (sim info)
         childNodes[0].GenerateText(sb);

         bool first;
         sb.AppendLine("///////////////// Rates //////////////////");

         if (Rates.Count > 0)
         {
            sb.Append("<< ");
            first = true;
            foreach (Rate r in Rates)
            {
               if (first)
                  first = false;
               else
                  sb.AppendLine(", ");

               r.GenerateText(sb);
            }
            sb.AppendLine(" >>");
         }
         sb.AppendLine();

         sb.AppendLine("//////////////// Declarations ///////////////////");
         sb.AppendLine();

         sb.AppendLine("//-------------- PProcs -------------- //");
         sb.AppendLine();

         Set<string> added = new Set<string>();
         //first pprocs!
         foreach (var dec in DecList)
         {
            if (dec.Value.PType == EntityType.PPROC ||
                dec.Value.PType == EntityType.PPROC_TEMPLATE)
            {
               dec.Value.GenerateText(sb);
               added.Add(dec.Key);
               sb.AppendLine();
            }
         }

         sb.AppendLine("//-------------- BioProcs -------------- //");
         sb.AppendLine();

         foreach (var dec in DecList)
         {
            if (dec.Value.PType == EntityType.BPROC ||
                dec.Value.PType == EntityType.BPROC_TEMPLATE)
            {
               dec.Value.GenerateText(sb);
               added.Add(dec.Key);
               sb.AppendLine();
            }
         }

         sb.AppendLine("//-------------------------------------- //");
         sb.AppendLine();

         foreach (var dec in DecList)
         {
            if (!added.Contains(dec.Key))
            {
               dec.Value.GenerateText(sb);
               sb.AppendLine();
            }
         }

         sb.AppendLine("//////////////// Run  ///////////////////");
         sb.AppendLine();

         sb.Append("run ");
         first = true;
         foreach (Pair<string, int> run in RunList)
         {
            if (first)
               first = false;
            else
               sb.Append(" || ");

            sb.Append(run.Second);
            sb.Append(" ");
            sb.Append(run.First);
         }

         sb.AppendLine();
      }

      public override Node Clone()
      {
         List<Pair<string, int>> newRunList = new List<Pair<string, int>>();
         List<Rate> newRates = new List<Rate>();
         Dictionary<string, Declaration> newDecList = new Dictionary<string, Declaration>();

         foreach (Pair<string, int> run in RunList)
            newRunList.Add(new Pair<string, int>(run.First, run.Second));

         foreach (Rate rate in Rates)
            newRates.Add((Rate)rate.Clone());

         foreach (var decl in DecList)
            newDecList.Add(decl.Key, (Declaration)decl.Value.Clone());

         return new Program(Position, childNodes[0].Clone(), newRunList, newRates, newDecList);
      }
   }

   public class Rate : NodeListElem<Rate>
   {
      public double Value = 0.0;
      string rateType;

      public string RateType
      {
         get { return rateType; }
         set { rateType = value; }
      }

      public Rate(Pos pos, string rateType, string rate)
         : base(pos)
      {
         if (rate.Equals("inf"))
            Value = Double.PositiveInfinity;
         else
            Double.TryParse(rate, System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo, out Value);

         this.rateType = rateType;
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void BuildList(ICollection<Rate> list)
      {
         list.Add(this);
      }

      public override void GenerateText(StringBuilder sb)
      {
         sb.Append("   ");
         sb.Append(rateType);
         sb.Append(" : ");
         if (Value == Double.PositiveInfinity)
            sb.Append("inf");
         else
            sb.Append(Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
      }

      public override Node Clone()
      {
         return new Rate(Position, RateType, (Value == Double.PositiveInfinity) ? "inf" : Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
      }
   }

   public class RateList : NodeList<Rate, Rate>
   {
      public RateList(Pos pos, Node rate)
         : base(pos)
      {
         childNodes.Add(rate);
      }

      public RateList(Pos pos, Node rate, Node rateList)
         : base(pos)
      {
         childNodes.Add(rate);
         childNodes.Add(rateList);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         if (childNodes.Count == 1)
            childNodes[0].GenerateText(sb);
         else
         {
            childNodes[0].GenerateText(sb);
            sb.Append(", ");
            childNodes[1].GenerateText(sb);
         }
      }

      public override Node Clone()
      {
         if (childNodes.Count == 1)
            return new RateList(Position, childNodes[0].Clone());
         else
            return new RateList(Position, childNodes[0].Clone(), childNodes[1].Clone());
      }
   }

   public class Declaration : NodeListElemWithID<Declaration>
   {
      EntityType processType;

      public string Name
      {
         get { return id; }
      }

      public EntityType PType
      {
         get { return processType; }
      }

      public PTN_Box BProc
      {
         get
         {
            if (processType == EntityType.BPROC)
               return (PTN_Box)childNodes[0];
            else
               return null;
         }
      }

      public PTN_Event Event
      {
         get
         {
            if (processType == EntityType.EVENT_DEF)
               return (PTN_Event)childNodes[0];
            else
               return null;
         }
      }

      public PiProcess PProc
      {
         get
         {
            if (processType == EntityType.PPROC)
               return (PiProcess)childNodes[0];
            else
               return null;
         }
         set
         {
            processType = EntityType.PPROC;
            childNodes[0] = value;
         }
      }

      public Molecule Molecule
      {
         get
         {
            if (processType == EntityType.MOL)
               return (Molecule)childNodes[0];
            else
               return null;
         }
      }

      public Declaration(Pos pos, string id, EntityType processType, Node process)
         : base(pos)
      {
         childNodes.Add(process);
         this.processType = processType;
         this.id = id;
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         switch (this.PType)
         {
            case EntityType.BPROC:
               sb.Append("let ");
               sb.Append(Name);
               sb.Append(" : bproc = ");
               childNodes[0].GenerateText(sb);
               sb.Append(";");
               break;            

            case EntityType.PPROC:
               sb.Append("let ");
               sb.Append(Name);
               sb.Append(" : pproc = ");
               childNodes[0].GenerateText(sb);
               sb.Append(";");
               break;

            case EntityType.MOL:
               sb.Append("let ");
               sb.Append(Name);
               sb.Append(" : complex = ");
               childNodes[0].GenerateText(sb);
               sb.Append(";");
               break;

            case EntityType.SEQUENCE:
               sb.Append("let ");
               sb.Append(Name);
               sb.Append(" : sequence = ");
               childNodes[0].GenerateText(sb);
               sb.Append(";");
               break;

            case EntityType.BPROC_TEMPLATE:
            case EntityType.PPROC_TEMPLATE:
            case EntityType.EVENT_DEF:
               childNodes[0].GenerateText(sb);
               break;

            default:
               Debug.Assert(false);
               break;
         }
         sb.AppendLine();
      }

      public override void BuildList(ICollection<Declaration> list)
      {
         list.Add(this);
      }

      private static int eventNo = 1;

      public override void BuildNamedList(IDictionary<string, Declaration> namedCollection)
      {
         if (this.PType == EntityType.EVENT_DEF)
         {
            // events do not have an ID
            namedCollection.Add("EVENT" + (++eventNo), this);
         }
         else
            namedCollection.Add(id, this);
      }

      public override Node Clone()
      {
         return new Declaration(Position, this.ID, this.PType, childNodes[0].Clone());
      }
   }

   public class DeclarationList : NodeListWithID<Declaration, Declaration>
   {
      public DeclarationList(Pos pos, Node declaration1, Node declaration2)
         : base(pos)
      {
         childNodes.Add(declaration1);
         childNodes.Add(declaration2);
      }

      public DeclarationList(Pos pos, Node declaration)
         : base(pos)
      {
         childNodes.Add(declaration);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         if (childNodes.Count == 1)
         {
            childNodes[0].GenerateText(sb);
         }
         else
         {
            childNodes[0].GenerateText(sb);
            sb.Append("\n\n");
            childNodes[1].GenerateText(sb);
         }
      }

      public override Node Clone()
      {
         if (childNodes.Count == 1)
            return new DeclarationList(Position, childNodes[0].Clone());
         else
            return new DeclarationList(Position, childNodes[0].Clone(), childNodes[1].Clone());

      }
   }


   //
   //Expression nodes
   //
   #region expressionnodes
   public class ExprAnd : Node
   {
      public ExprAnd(Pos pos, Node expression1, Node expression2)
         : base(pos)
      {
         childNodes.Add(expression1);
         childNodes.Add(expression2);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         childNodes[0].GenerateText(sb);
         sb.Append(" and ");
         childNodes[1].GenerateText(sb);
      }

      public override Node Clone()
      {
         return new ExprAnd(Position, childNodes[0], childNodes[1]);
      }
   }

   public class ExprOr : Node
   {
      public ExprOr(Pos pos, Node expression1, Node expression2)
         : base(pos)
      {
         childNodes.Add(expression1);
         childNodes.Add(expression2);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         childNodes[0].GenerateText(sb);
         sb.Append(" or ");
         childNodes[1].GenerateText(sb);
      }

      public override Node Clone()
      {
         return new ExprOr(this.Position, childNodes[0], childNodes[1]);
      }
   }

   public class ExprNot : Node
   {
      public ExprNot(Pos pos, Node expression)
         : base(pos)
      {
         childNodes.Add(expression);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         sb.Append("not ");
         childNodes[0].GenerateText(sb);
      }

      public override Node Clone()
      {
         return new ExprNot(Position, childNodes[0]);
      }
   }

   public class Atom : Node
   {
      BinderState binderState;
      List<string> atoms = new List<string>();

      public Atom(Pos pos, string atom1, string atom2, BinderState state)
         : base(pos)
      {
         atoms.Add(atom1);
         atoms.Add(atom2);
         binderState = state;
      }

      public Atom(Pos pos, string atom1, BinderState state)
         : base(pos)
      {
         atoms.Add(atom1);
         binderState = state;
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         sb.Append("(");
         sb.Append(atoms[0]);

         if (atoms.Count > 1)
         {
            sb.Append(", ");
            sb.Append(atoms[1]);
         }

         switch (binderState)
         {
            case BinderState.STATE_BOUND:
               sb.Append(", bound");
               break;

            case BinderState.STATE_HIDDEN:
               sb.Append(", hidden");
               break;

            case BinderState.STATE_UNHIDDEN:
               sb.Append(", unhidden");
               break;
         }

         sb.Append(")");
      }

      public override Node Clone()
      {
         if (atoms.Count == 1)
            return new Atom(Position, atoms[0], binderState);
         return new Atom(Position, atoms[0], atoms[1], binderState);
      }
   }

   #endregion expressionnodes

   //
   //Action nodes
   //
   #region actionnodes

   public class ActionTau : PiProcess
   {
      public double Rate;

      public ActionTau(Pos pos, string rate)
         : base(pos)
      {
         if (rate.Equals("inf"))
            Rate = Double.PositiveInfinity;
         else
            Rate = Double.Parse(rate, System.Globalization.NumberFormatInfo.InvariantInfo);
      }

      public ActionTau(Pos pos, double rate)
         : base(pos)
      {
         Rate = rate;
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         if (Rate == Double.PositiveInfinity)
            sb.Append("delay(inf)");
         else
         {
            sb.Append("delay(");
            sb.Append(Rate.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(")");
         }
      }

      public override Node Clone()
      {
         return new ActionTau(Position, Rate);
      }
   }

   public class ActionDie : PiProcess
   {
      public double Rate;

      public ActionDie(Pos pos, string rate)
         : base(pos)
      {
         if (rate.Equals("inf"))
            Rate = Double.PositiveInfinity;
         else if (rate.Equals(""))
            Rate = 0.0;
         else
            Rate = Double.Parse(rate, System.Globalization.NumberFormatInfo.InvariantInfo);
      }

      public ActionDie(Pos pos, double rate)
         : base(pos)
      {
         Rate = rate;
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         if (Rate == Double.PositiveInfinity)
            sb.Append("die(inf)");
         else if (Rate == 0.0)
            sb.Append("die");
         else
         {
            sb.Append("die(");
            sb.Append(Rate.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(")");
         }
      }

      public override Node Clone()
      {
         return new ActionDie(Position, Rate);
      }
   }


   public class ActionIO : PiProcess
   {
      public string ChannelName { get; set; }
      public string ParamName { get; set; }

      public ActionType AType { get; set; }

      public ActionIO()
         : base(Pos.Empty)
      {
         AType = ActionType.OUTPUT;
         ChannelName = "x";
         ParamName = "";
      }

      public ActionIO(Pos pos, string channelName, string paramName, ActionType actionType)
         : base(pos)
      {
         AType = actionType;
         ChannelName = channelName;
         ParamName = paramName;
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         switch (AType)
         {
            case ActionType.INPUT:
               sb.Append(ChannelName);
               sb.Append("?(");
               sb.Append(ParamName);
               sb.Append(")");
               break;

            case ActionType.OUTPUT:
               sb.Append(ChannelName);
               sb.Append("!(");
               if (ParamName != "$empty_output")
                  sb.Append(ParamName);
               sb.Append(")");
               break;

            case ActionType.HIDE:
               sb.Append("hide(");
               sb.Append(ChannelName);
               sb.Append(")");
               break;

            case ActionType.UNHIDE:
               sb.Append("unhide(");
               sb.Append(ChannelName);
               sb.Append(")");
               break;
         }
      }

      public override Node Clone()
      {
         return new ActionIO(Position, ChannelName, ParamName, AType);
      }
   }

   public class ActionExpose : PiProcess
   {
      string rate;
      string channelName;
      string channelRate;
      string channelType;

      public ActionExpose(Pos pos, string exposeRate, string channelName, string channelRate, string channelType)
         : base(pos)
      {
         this.channelName = channelName;
         this.channelRate = channelRate;
         this.channelType = channelType;
         this.rate = exposeRate;
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         sb.Append("expose(");
         if (!rate.Equals(""))
         {
            sb.Append(rate);
            sb.Append(", ");
         }
         sb.Append(channelName);
         sb.Append(":");
         sb.Append(channelRate);
         sb.Append("; ");
         sb.Append(channelType);
         sb.Append(")");
      }

      public override Node Clone()
      {
         return new ActionExpose(Position, rate, channelName, channelRate, channelType);
      }
   }

   public class ActionHU : PiProcess
   {
      public string Rate { get; set; }
      public string Name { get; set; }

      public ActionType AType { get; set; }

      public ActionHU()
         : base(Pos.Empty)
      {
         AType = ActionType.HIDE;
         Rate = "";
         Name = "";
      }

      public ActionHU(Pos pos, string rate, string name, ActionType actionType)
         : base(pos)
      {
         AType = actionType;
         this.Rate = rate;
         this.Name = name;
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         switch (AType)
         {
            case ActionType.HIDE:
               if (Rate.Equals(""))
               {
                  sb.Append("hide(");
                  sb.Append(Name);
                  sb.Append(")");
               }
               else
               {
                  sb.Append("hide(");
                  sb.Append(Rate);
                  sb.Append(", ");
                  sb.Append(Name);
                  sb.Append(")");
               }
               break;

            case ActionType.UNHIDE:
               if (Rate.Equals(""))
               {
                  sb.Append("unhide(");
                  sb.Append(Name);
                  sb.Append(")");
               }
               else
               {
                  sb.Append("unhide(");
                  sb.Append(Rate);
                  sb.Append(", ");
                  sb.Append(Name);
                  sb.Append(")");
               }
               break;
         }
      }

      public override Node Clone()
      {
         return new ActionHU(Position, Rate, Name, AType);
      }
   }

   public class ActionChange : PiProcess
   {
      public string Rate;
      public string Name;
      public string Type;

      public ActionChange(Pos pos, string rate, string name, string type)
         : base(pos)
      {
         this.Rate = rate;
         this.Name = name;
         this.Type = type;
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         sb.Append("ch(");
         if (!Rate.Equals(""))
         {
            sb.Append(Rate);
            sb.Append(", ");
         }
         sb.Append(Name);
         sb.Append(", ");
         sb.Append(Type);
         sb.Append(")");
      }

      public override Node Clone()
      {
         return new ActionChange(Position, Rate, Name, Type);
      }
   }

   public class ActionList : PiProcess
   {
      public ActionList()
         : base(Pos.Empty) { }

      public ActionList(Pos pos, Node action)
         : base(pos)
      {
         childNodes.Add(action);
      }

      public ActionList(Pos pos, Node action, Node actionList)
         : base(pos)
      {
         childNodes.Add(action);
         childNodes.Add(actionList);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         switch (childNodes.Count)
         {
            case 0:
               break;
            case 1:
               childNodes[0].GenerateText(sb);
               break;
            default:
               childNodes[0].GenerateText(sb);
               for (int i = 1; i < childNodes.Count; ++i)
               {
                  sb.Append(".");
                  childNodes[i].GenerateText(sb);
               }
               break;
         }
      }

      public override Node Clone()
      {
         if (childNodes.Count == 1)
            return new ActionList(Position, childNodes[0].Clone());
         else
            return new ActionList(Position, childNodes[0].Clone(), childNodes[1].Clone());
      }
   }

   #endregion actionnodes

   //
   // Beta nodes
   //
   #region betanodes

   public class PTN_Box : Node
   {

      private bool isImmediate;
      public bool IsImmediate
      {
         get { return isImmediate; }
         set { isImmediate = value; }
      }

      public PiProcess PProc
      {
         get { return (PiProcess)childNodes[1]; }
      }

      /*public Beta(Pos pos, Node binder, Node piprocess, List<Binder> otherBinders)
         : base(pos)
      {
         childNodes.Add(binder);
         childNodes.Add(piprocess);
         binders.AddRange(otherBinders);         
      }*/

      public PTN_Box(Pos pos, Node binder, Node piprocess)
         : base(pos)
      {
         IsImmediate = false;
         childNodes.Add(binder);
         childNodes.Add(piprocess);
         processBinders(binder);
      }

      private void processBinders(Node node)
      {
         if (node is BinderNode)
         {
            binders.Add((BinderNode)node);
         }
         else
         {
            ((BinderPair)node).AddAll(binders);
         }
      }

      List<BinderNode> binders = new List<BinderNode>();

      public List<BinderNode> Binders
      {
         get { return binders; }
         set { binders = value; }
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         //binder LSOPEN par LSCLOSE
         bool first = true;
         foreach (var binder in binders)
         {
            if (first)
               first = false;
            else
               sb.Append(", ");

            binder.GenerateText(sb);
         }
         sb.AppendLine();
         sb.Append("[ ");
         childNodes[1].GenerateText(sb);
         sb.Append(" ]");
      }

      public override Node Clone()
      {
         return new PTN_Box(Position, childNodes[0].Clone(), childNodes[1].Clone());
      }

      public void SetPProc(PiProcess newPProc)
      {
         childNodes[1] = newPProc;
      }
   }

   public class BinderNode : Node
   {
      public string BinderSubject;
      public string BinderRate;
      public string BinderTypeValue;
      public string BinderTypeName;

      public BinderState BinderState;

      public BinderNode(Pos pos, string subject, string binderValue, string binderIdentifier, BinderState binderState, string rate)
         : base(pos)
      {
         this.BinderSubject = subject;
         this.BinderTypeValue = binderValue;
         this.BinderTypeName = binderIdentifier;
         this.BinderState = binderState;
         this.BinderRate = rate;
      }


      public BinderNode(Pos pos, string subject, string binderValue, BinderState binderState, string rate)
         : this(pos, subject, binderValue, "string", binderState, rate)
      {
      }


      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }
   
      public override void  GenerateText(StringBuilder sb)
      {
         switch (this.BinderState)
         {
            case BinderState.STATE_BOUND:
               sb.Append("#c");
               break;

            case BinderState.STATE_HIDDEN:
               sb.Append("#h");
               break;

            case BinderState.STATE_UNHIDDEN:
               sb.Append("#");
               break;
         }

         sb.Append("(");
         sb.Append(BinderSubject);

         if (!String.IsNullOrEmpty(BinderRate))
         {
            sb.Append(":");
            sb.Append(BinderRate);
         }

         sb.Append(", ");
         sb.Append(BinderTypeValue);
         sb.Append(")");

 	     
         //LBB LPOPEN LID LDDOT rate LCOMMA LID LPCLOSE
      }

      public override Node Clone()
      {
 	      return new BinderNode(this.pos, 
            this.BinderSubject,
            this.BinderTypeValue,
            this.BinderTypeName,
            this.BinderState,
            this.BinderRate);
      }
   }


   public class BinderPair : Node
   {
      public BinderPair(Pos pos, Node binder1, Node binder2)
         : base(pos)
      {
         childNodes.Add(binder1);
         if (binder2 != null)
         {
            childNodes.Add(binder2);
         }
      }

      internal void AddAll(List<BinderNode> binders)
      {
         foreach (Node node in childNodes)
         {
            if (node is BinderNode)
            {
               binders.Add((BinderNode)node);
            }
            else
            {
               ((BinderPair)node).AddAll(binders);
            }
         }
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         if (childNodes.Count == 1)
         {
            childNodes[0].GenerateText(sb);
         }
         else
         {
            childNodes[0].GenerateText(sb);
            sb.Append(", ");
            childNodes[1].GenerateText(sb);
         }
      }

      public override Node Clone()
      {
         if (childNodes.Count == 1)
         {
            return new BinderPair(Position, childNodes[0].Clone(), null);
         }
         else
         {
            return new BinderPair(Position, childNodes[0].Clone(), childNodes[1].Clone());
         }
      }
   }

   public class Bp : NodeListElem<Pair<string, int>>
   {
      private string ID;
      private int Quantity;

      public Bp(Pos pos, string id, string num)
         : base(pos)
      {
         ID = id;
         Quantity = Int32.Parse(num);
      }

      public override void BuildList(ICollection<Pair<string, int>> list)
      {
         list.Add(new Pair<string, int>(ID, Quantity));
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         sb.Append(Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture));
         sb.Append(" ");
         sb.Append(ID);
      }

      public override Node Clone()
      {
         return new Bp(Position, ID, Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture));
      }
   }

   public class BpList : NodeList<Bp, Pair<string, int>>
   {
      public BpList(Pos pos, Node bp)
         : base(pos)
      {
         childNodes.Add(bp);
      }

      public BpList(Pos pos, Node bp, Node bpList)
         : base(pos)
      {
         childNodes.Add(bp);
         childNodes.Add(bpList);
      }

      public override void GenerateText(StringBuilder sb)
      {
         childNodes[0].GenerateText(sb);
         if (childNodes.Count > 1)
         {
            sb.Append(" || ");
            childNodes[1].GenerateText(sb);
         }
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override Node Clone()
      {
         if (childNodes.Count == 1)
            return new BpList(Position, childNodes[0].Clone());
         else
            return new BpList(Position, childNodes[0].Clone(), childNodes[1].Clone());
      }
   }

   #endregion betanodes

   //
   // Molecule nodes
   //
   #region molnodes


   public struct MolSignatureElem
   {
      public string node1;
      public string binder1;
      public string node2;
      public string binder2;
   }

   public class Molecule : Node
   {

      private bool isImmediate;
      public bool IsImmediate
      {
         get { return isImmediate; }
         set { isImmediate = value; }
      }

      public Molecule(Pos pos, Node signature, Node molNodeList)
         : base(pos)
      {
         childNodes.Add(signature);
         childNodes.Add(molNodeList);

         ((MolEdgeList)signature).BuildList(MolSignature);
         ((MolNodeList)molNodeList).BuildList(NodeList);

         // find types for nodes that have them
         Dictionary<string, MolNode> defindedNodes = new Dictionary<string, MolNode>();

         // first pass, store defined
         foreach (MolNode molNode in NodeList)
         {
            if (molNode.BoxType != null)
            {
               defindedNodes.Add(molNode.SubName, molNode);
            }
         }

         // second pass, set stored
         foreach (MolNode molNode in NodeList)
         {
            if (molNode.BoxType == null)
            {
               molNode.BoxType = defindedNodes[molNode.RefType].BoxType;
               molNode.Binders = defindedNodes[molNode.RefType].Binders;
               defindedNodes.Add(molNode.SubName, molNode);
            }
         }
      }

      public List<MolSignatureElem> MolSignature = new List<MolSignatureElem>();

      public List<MolNode> NodeList = new List<MolNode>();

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         //LGOPEN mol_signature LDOTCOMMA node_list LGCLOSE
         sb.AppendLine("{");
         childNodes[0].GenerateText(sb);
         sb.AppendLine();
         childNodes[1].GenerateText(sb);
         sb.AppendLine();
         sb.AppendLine("}");
      }

      public override Node Clone()
      {
         throw new NotImplementedException();
      }
   }

   public class MolEdgeList : NodeList<MolEdge, MolSignatureElem>
   {
      public MolEdgeList(Pos pos, Node edge)
         : this(pos, edge, null)
      {
      }

      public MolEdgeList(Pos pos, Node edge, Node edgeList)
         : base(pos)
      {
         childNodes.Add(edge);
         if (edgeList != null)
            childNodes.Add(edgeList);
      }

      /*
      public void BuildSignature(List<MolSignatureElem> MolSignature)
      {
         ((MolEdge)childNodes[0]).BuildSignature(MolSignature);
         if (childNodes.Count > 1)
            ((MolEdgeList)childNodes[1]).BuildSignature(MolSignature);
      }
      */

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         childNodes[0].GenerateText(sb);
         if (childNodes.Count > 1)
         {
            sb.Append(", ");
            childNodes[1].GenerateText(sb);
         }
      }

      public override Node Clone()
      {
         if (childNodes.Count == 1)
            return new MolEdgeList(Position, childNodes[0]);
         return new MolEdgeList(Position, childNodes[0], childNodes[1]);
      }
   }

   public class MolEdge : NodeListElem<MolSignatureElem>
   {
      MolSignatureElem SignatureElem = new MolSignatureElem();

      public MolEdge(Pos pos, string entity1, string binder1, string entity2, string binder2)
         : base(pos)
      {
         SignatureElem.node1 = entity1;
         SignatureElem.node2 = entity2;
         SignatureElem.binder1 = binder1;
         SignatureElem.binder2 = binder2;
      }

      public override void BuildList(ICollection<MolSignatureElem> MolSignature)
      {
         MolSignature.Add(SignatureElem);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         //LPOPEN LID LCOMMA LID LCOMMA LID LCOMMA LID LPCLOSE 
         sb.Append("(");

         sb.Append(SignatureElem.node1);
         sb.Append(", ");
         sb.Append(SignatureElem.binder1);

         sb.Append(SignatureElem.node2);
         sb.Append(", ");
         sb.Append(SignatureElem.binder2);

         sb.Append(")");
      }

      public override Node Clone()
      {
         throw new NotImplementedException();
      }
   }

   public class MolNodeList : NodeList<MolNode, MolNode>
   {
      public MolNodeList(Pos pos, Node node)
         : this(pos, node, null)
      {
      }

      public MolNodeList(Pos pos, Node node, Node nodeList)
         : base(pos)
      {
         childNodes.Add(node);
         if (nodeList != null)
            childNodes.Add(nodeList);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      /*
      internal void BuildList(List<MolNode> NodeList)
      {
         NodeList.Add((MolNode)childNodes[0]);
         if (childNodes.Count > 1)
         {
            ((MolNodeList)childNodes[1]).BuildList(NodeList);
         }
      }
       */

      public override void GenerateText(StringBuilder sb)
      {
         childNodes[0].GenerateText(sb);
         if (childNodes.Count > 1)
         {
            childNodes[1].GenerateText(sb);
         }
      }

      public override Node Clone()
      {
         throw new NotImplementedException();
      }
   }

   public class MolNode : NodeListElem<MolNode>
   {
      string boxTypeName = null;
      string subName = null;

      public string SubName
      {
         get { return subName; }
         set { subName = value; }
      }
      string refType = null;

      public string RefType
      {
         get { return refType; }
         set { refType = value; }
      }

      public MolNode(Pos pos, string name, string baseName, Node binderList)
         : base(pos)
      {
         childNodes.Add(binderList);
         //if (BetaSim.Data.Env.Get.Boxes.Exists(baseName))
         //{
         boxTypeName = baseName;
         subName = name;
         ((MolBinderList)binderList).BuildList(blist);
         //}
         //else
         //{
         //   BetaSim.Data.Env.Get.Errors.PrintError(pos.line, pos.col, baseName + " not declared as a Beta process type.");
         //}
      }

      public MolNode(Pos pos, string name, string eqName)
         : base(pos)
      {
         subName = name;
         refType = eqName;
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void BuildList(ICollection<MolNode> list)
      {
         list.Add(this);
      }

      public string BoxType
      {
         get { return boxTypeName; }
         set { boxTypeName = value; }
      }

      List<string> blist = new List<string>();

      public List<string> Binders
      {
         get { return blist; }
         set { blist = value; }
      }

      public override void GenerateText(StringBuilder sb)
      {
         if (refType == null)
         {
            //LID LDDOT LID LEQUAL LPOPEN mol_binder_list LPCLOSE LDOTCOMMA 
            sb.Append(subName);
            sb.Append(" : ");
            sb.Append(boxTypeName);
            sb.Append(" = (");
            childNodes[0].GenerateText(sb);
            sb.Append(");");
         }
         else
         {
            //LID LEQUAL LID LDOTCOMMA   
            sb.Append(subName);
            sb.Append(" : ");
            sb.Append(refType);
            sb.Append(";");
         }
      }

      public override Node Clone()
      {
         throw new NotImplementedException();
      }
   }

   public class MolBinder : NodeListElem<string>
   {
      string name;

      public string Name
      {
         get { return name; }
         set { name = value; }
      }

      public MolBinder(Pos pos, string binderName)
         : base(pos)
      {
         name = binderName;
      }

      public override void BuildList(ICollection<string> list)
      {
         list.Add(name);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         sb.Append(name);
      }

      public override Node Clone()
      {
         throw new NotImplementedException();
      }
   }

   public class MolBinderList : NodeList<MolBinder, string>
   {
      public MolBinderList(Pos pos, Node binder, Node otherBinders)
         : base(pos)
      {
         childNodes.Add(binder);
         childNodes.Add(otherBinders);
      }

      public MolBinderList(Pos pos, Node binder)
         : base(pos)
      {
         childNodes.Add(binder);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         childNodes[0].GenerateText(sb);
         if (childNodes.Count > 1)
         {
            sb.Append(", ");
            childNodes[1].GenerateText(sb);
         }
      }

      public override Node Clone()
      {
         throw new NotImplementedException();
      }
   }

   #endregion molnodes

   //
   // Event nodes
   //

}
