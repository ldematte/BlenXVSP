using System.Text;
using System.Collections.Generic;

namespace Dema.BlenX.Parser
{
	
	public class Mapping : NodeListElem<Mapping> {
		//BinderPair binders;
		string name;
		//it is true if the number of the binder reuqired by the rule
		//is not fixed.
		bool fixedBinderNumber;
		
		public CompressedBinderDescriptionList CompressedBinderDescriptions {
			get{
				return (CompressedBinderDescriptionList) childNodes[0];
			}
		}
		
		public string AssociatedName{
			get{
				return name;
			}
		}
		
		public bool FixedBinderNumber{
			get {
				return fixedBinderNumber;
			}
		}
		
		public Mapping(Pos pos, Node binders, bool fixedBinderNumber, string name) : base(pos) {
			childNodes.Add(binders);
			this.name = name;
			this.fixedBinderNumber = fixedBinderNumber;
		}
		
		public override Node Clone () {
			return new Mapping(Position, childNodes[0].Clone(), fixedBinderNumber, name);
		}

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

		public override void GenerateText (StringBuilder sb) {
			sb.Append("(");
			childNodes[0].GenerateText(sb);
			if (fixedBinderNumber) {
				sb.Append("...");
			}
			sb.Append(") : ");
			sb.Append(name);
			sb.Append(";\n");
		}
	
		public override void  BuildList(ICollection<Mapping> list) {
   			list.Add(this);
		}
		
	}
	
	public class MappingsList : NodeList<Mapping,Mapping> {
		public MappingsList(Pos pos, Node map) : base(pos) {
			this.childNodes.Add(map);
		}
		
		public MappingsList(Pos pos, Node map, Node mapList) : base (pos) {
			this.childNodes.Add(map);
			this.childNodes.Add(mapList);
		}

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }
		
		public override Node Clone ()
		{
			if (childNodes.Count == 1){
				return new MappingsList(Position, (Mapping)childNodes[0].Clone());
			} else {
				return new MappingsList(Position, (Mapping)childNodes[0].Clone(), (MappingsList)childNodes[1].Clone());
			}
		}
		
		public override void GenerateText (StringBuilder sb)
		{
			if (childNodes.Count == 1){
				sb.Append("(");
				childNodes[0].GenerateText(sb);
				sb.Append(")");
			} else {
				childNodes[0].GenerateText(sb);
	            sb.Append("\n");
	            childNodes[1].GenerateText(sb);
	        }
		}
	}
	
	public class Mapt : Node {
			
		public MappingsList Mappings{
			get{
				return (MappingsList) childNodes[0];
			}
	    }
		
		public Mapt(Pos pos, Node mappingsList) : base(pos) {
			childNodes.Add(mappingsList);
		}
		
		public override Node Clone () {
			return childNodes[0].Clone();
		}

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

		public override void GenerateText (StringBuilder sb)
		{
			childNodes[0].GenerateText(sb);
		}
	}
	
	public class CompressedBinderDescription : NodeListElem<CompressedBinderDescription>
	{
	
		public VarList TypeList
		{
			get
			{
				return (VarList) childNodes[0];
			}
		}
		
		public BinderStateElemList StateList
		{
			get
			{
				return (BinderStateElemList) childNodes[1];
			}
		}	
		
	 	public CompressedBinderDescription (Pos pos, Node typeList, Node stateList)
		: base(pos)
		{
			childNodes.Add(typeList);
			childNodes.Add(stateList);
		}
		
		public override void Accept(INodeVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                foreach (Node childNode in childNodes) 
				{
					if (childNode != null)
					{
				   		childNode.Accept(visitor);
					}
				}
			}
        }

        public override void GenerateText(StringBuilder sb)
        {
			if (childNodes[0] != null) {
				childNodes[0].GenerateText(sb);
			} else {
				sb.Append("*");
			}
			sb.Append(":");
            if (childNodes[1] != null) {
				childNodes[1].GenerateText(sb);
			} else {
				sb.Append("*");
			}
        }

        public override Node Clone()
        {
			
			Node child0 = null;
			Node child1 = null;
			
			if (childNodes[0] != null){
				child0 = childNodes[0].Clone();
			}
			
			if (childNodes[1] != null){
				child0 = childNodes[1].Clone();
			}
			
            return new CompressedBinderDescription(Position, child0.Clone(), child1.Clone());
        }
		
				
		public override void  BuildList(ICollection<CompressedBinderDescription> list) {
   			list.Add(this);
		}
	}
	
	public class CompressedBinderDescriptionList : NodeList<CompressedBinderDescription, CompressedBinderDescription>
	{
		public CompressedBinderDescriptionList (Pos pos, Node cbd)
        : base(pos)
        {
            childNodes.Add(cbd);
        }
		
	    public CompressedBinderDescriptionList(Pos pos, Node cbd, Node cbdList)
        : base(pos)
        {
            childNodes.Add(cbd);
            childNodes.Add(cbdList);
        }


        public override void GenerateText(StringBuilder sb)
        {
			
			childNodes[0].GenerateText(sb);
			sb.Append(";\n");
			if (childNodes.Count > 1)
            {
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
	
	public class BinderStateElem : NodeListElem<BinderState>
	{
		private BinderState bs;
		public BinderState Bs
        {
            get
            {
                return bs;
            }
		}
		
		public BinderStateElem(Pos pos, BinderState bs)
		: base(pos)
		{
			this.bs = bs;
		}
		
		public override void Accept(INodeVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                foreach (Node childNode in childNodes)
                    childNode.Accept(visitor);
            }
        }

        public override void BuildList(ICollection<BinderState> list)
        {
            list.Add(this.Bs);
        }

        public override void GenerateText(StringBuilder sb)
        {
            sb.Append(bs.ToString());
        }

        public override Node Clone()
        {
            return new BinderStateElem(Position, bs);
        }		
			
	}
	
	public class BinderStateElemList : NodeList<BinderStateElem, BinderState>
	{
		public BinderStateElemList(Pos pos, Node bs)
        : base(pos)
        {
            childNodes.Add(bs);
        }

        public BinderStateElemList(Pos pos, Node bs, Node bsList)
        : base(pos)
        {
            childNodes.Add(bs);
            childNodes.Add(bsList);
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
                return new BinderStateElemList(Position, childNodes[0].Clone());
            else
                return new BinderStateElemList(Position, childNodes[0].Clone(), childNodes[1].Clone());
        }
	}
		
}