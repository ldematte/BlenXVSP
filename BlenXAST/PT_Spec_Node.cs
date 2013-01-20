using System.Text;
using System.Collections.Generic;

namespace Dema.BlenX.Parser
{
	
	public enum SpecReactionType{
		MONO,
		BIND,
		BIMBIND,
		SPLIT,
		UNBIND,
		JOIN,
		NEW,
		DELETE,
		UPDATE,
      BIM
	}
	
	public class Reaction : NodeListElem<Reaction>
   {
		SpecReactionType reactionType;
		
      public List<string> Reactants;
		public List<string> Products;
		int nFired;
		
		public SpecReactionType ReactionType{
			get{
				return reactionType;
			}
	    }
		
		public Reaction(Pos pos, SpecReactionType reactionType, List<string> reactants, List<string> products, int nFired) : base (pos){
			this.reactionType = reactionType;
			this.Reactants = reactants;
			this.Products = products;
			this.nFired = nFired;
		}

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }
		
		public override void GenerateText(StringBuilder sb){
			foreach (string reactant in Reactants){
			    sb.Append(reactant);
				sb.Append(" ");
			}
			
			sb.Append(" ");
			sb.Append("--(");
			sb.Append(reactionType.ToString().ToLower());
			sb.Append(")-> ");
			
			foreach (string product in Products){
			    sb.Append(product);
				sb.Append(" ");
			}
			sb.Append(" ");
			sb.Append("[");
			sb.Append(nFired);
			sb.Append("]");
			sb.Append("\n");			        
		}

      public override string ToString()
      {
         StringBuilder sb = new StringBuilder();
         this.GenerateText(sb);
         return sb.ToString();
      }
	
		public override void  BuildList(ICollection<Reaction> list){
   			list.Add(this);
		}
		
		public override Node Clone(){
			List<string> reactantsCopy = new List<string>(Reactants);
			List<string> productsCopy = new List<string>(Products);
			return new Reaction(Position, reactionType, reactantsCopy, productsCopy, nFired);
		}	
	}
	
	public class Var : NodeListElem<Var>{
		private string name;
		
		public string Name{
			get {
				return name;
			}
		}
		
		public Var(Pos pos, string name) : base(pos){
			this.name = name;
		}
		
		public override void GenerateText(StringBuilder sb){
			sb.Append(name);
		}
		
		public override void  BuildList(ICollection<Var> list){
   			list.Add(this);
		}

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }
		
		public override Node Clone(){
			return new Var(Position, name);
		}	
		
	}
	
	public class VarList : NodeList<Var,Var>{
		public VarList(Pos pos, Node var) : base(pos){
			childNodes.Add(var);
		}
		
		public VarList(Pos pos, Node var, Node varList) : base(pos){
			childNodes.Add(var);
			childNodes.Add(varList);
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
			if (childNodes.Count == 1){
				childNodes[0].GenerateText(sb);
			} else {
				childNodes[0].GenerateText(sb);
	            sb.Append("\n");
	            childNodes[1].GenerateText(sb);
	        }
		}
		
		public override Node Clone()
		{
			if (childNodes.Count == 1){
				return new VarList(Position, childNodes[0].Clone());
			} else {
				return new VarList(Position, childNodes[0].Clone(), childNodes[1].Clone());
			}
		}
	}
		
	
	public class ReactionList : NodeList<Reaction,Reaction>{
		
		public ReactionList(Pos pos, Node reaction) : base(pos){
			childNodes.Add(reaction);
		}
		
		public ReactionList(Pos pos, Node reaction, Node reactionList) : base(pos){
			childNodes.Add(reaction);
			childNodes.Add(reactionList);
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
			if (childNodes.Count == 1){
				childNodes[0].GenerateText(sb);
			} else {
				childNodes[0].GenerateText(sb);
	            sb.Append("\n");
	            childNodes[1].GenerateText(sb);
	        }
		}
		
		
		public override Node Clone()
		{
			if (childNodes.Count == 1){
				return new ReactionList(Position, childNodes[0].Clone());
			} else {
				return new ReactionList(Position, childNodes[0].Clone(), childNodes[1].Clone());
			}
		}
		
	}
	
		
	public class Species : Node{
		private ReactionList reactionsList;
		private DeclarationList entitiesList;
		private DeclarationList complexesList;
		private VarList variablesList;
		
		
		public ReactionList Reactions{
			get{
				return reactionsList;
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
		
		public DeclarationList Entities{
			get{
				return entitiesList;
			}
	    }
		
		public DeclarationList Complexes{
			get{
				return complexesList;
			}
	    }
		
		public  VarList Variables{
			get{
				return variablesList;
			}
	    }
		
		public Species(Pos pos, ReactionList reactionsList, DeclarationList entitiesList, DeclarationList complexesList, VarList variablesList) : base(pos){
			this.reactionsList = reactionsList;
			this.entitiesList = entitiesList;	
			this.complexesList = complexesList;
			this.variablesList = variablesList;
		}
		
		public override void GenerateText(StringBuilder sb)
		{
			sb.Append("REACTIONS\n\n");
			if (reactionsList != null) {
				reactionsList.GenerateText(sb);
			} else {
				sb.Append("ciao");
			}
			
			sb.Append("\n\nENTITIES\n\n");
			if (entitiesList != null) {
				entitiesList.GenerateText(sb);
			}
			sb.Append("\n\nCOMPLEXES\n\n");
			if (complexesList != null) {
				complexesList.GenerateText(sb);
			}
			sb.Append("\n\nVARIBLES\n\n");
			if (variablesList != null){
				variablesList.GenerateText(sb);
			}
		}
		
		public override Node Clone(){
			return new Species(Position, (ReactionList)reactionsList.Clone(), (DeclarationList)entitiesList.Clone(), (DeclarationList)complexesList.Clone(), (VarList)variablesList.Clone());
		}
	}

}
