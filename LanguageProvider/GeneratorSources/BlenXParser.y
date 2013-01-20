%using Microsoft.VisualStudio.TextManager.Interop
%using Dema.BlenX.VisualStudio
%using Dema.BlenX.Parser
%using System.Diagnostics
%namespace Babel.Parser
%valuetype LexValue
%partial


%{
	SymbolTable st = null;
	ErrorHandler handler = null;
	string currentFile;

	public void SetParsingInfo(string fileName, SymbolTable symbolTable, ErrorHandler hdlr) 
	{ 
		this.st = symbolTable; 
		this.currentFile = fileName;
		st.RemoveFile(currentFile);
		CurrentScope = BlenXScopeType.InsideNothing;
	}    
	
	public BlenXScopeType CurrentScope = BlenXScopeType.InsideNothing;
	
	public SymbolTable SymbolTable { get { return st; } }
    
   internal void CallErrorHandler(string msg, LexLocation val)
   {
      if (handler != null)
         handler.AddError(msg, val.sLin, val.sCol, val.eCol - val.sCol);
   }
    
    internal TextSpan ToTextSpan(LexLocation s) { return TextSpan(s.sLin, s.sCol, s.eLin, s.eCol); }

    internal void Match(LexLocation lh, LexLocation rh) 
    {
       //if this is not a ParseRequest (interactive), ignore
       if (request != null)
          DefineMatch(ToTextSpan(lh), ToTextSpan(rh)); 
    }
    
    internal void AddOutliningRegion(LexLocation lh, LexLocation rh) 
    { 
       //if this is not a ParseRequest (interactive), ignore
       if (request != null)
       {
          if (Sink.HiddenRegions == true)
          {
              TextSpan hideSpan = new TextSpan(); 
              TextSpan lHand = ToTextSpan(lh); 
              TextSpan rHand = ToTextSpan(rh); 
              
              hideSpan.iStartIndex = lHand.iEndIndex; 
              hideSpan.iStartLine = lHand.iStartLine; 
              hideSpan.iEndIndex = rHand.iEndIndex; 
              hideSpan.iEndLine = rHand.iEndLine; 
              
              Sink.ProcessHiddenRegions = true; 
              Sink.AddHiddenRegion(hideSpan); 
           }
        }
     }
%}



%union
{
   public string ptr_string;
   public Node tree_node; 
}


%token <ptr_string> LID 
%token <ptr_string> LDECIMAL
%token <ptr_string> LREAL
%token <ptr_string> LINF

%token LSTEPS LSTEP LAOPEN LACLOSE LGOPEN LGCLOSE LNIL LBNIL LPARALLEL LPOPEN LPCLOSE 
       LCHOICE LRESTRICTION LSOPEN LSCLOSE LEQUAL LNEQUAL LDOT LDDOT LCOMMA LBB LBBH LDOTCOMMA 
       LDAOPEN LDACLOSE LLET LBASERATE LREXPOSE LRHIDE LRUNHIDE LDELIM LTAU LDELTA
       LTIME LTIMESPAN LCHANGE LDIE LRCHANGE LRATE LQM LEM
       
%token LSTATEUNHIDE LSTATEHIDE LSTATEBOUND
       
%token LWHEN LINHERIT LNEW LSPLIT LDELETE LJOIN LLEFTARROW LRIGHTARROW LCHANGED LUPDATE 
       
%token LMIN LIF LTHEN LAND LOR LNOT LP1 LENDIF
	   LP2 LFJOIN LFSPLIT LBOTTOM LIDENTITY

%token LBPARALLEL
%token LTYPE LPIPROCESS LBETAPROCESS LMOLECULE LPREFIX LTEMPLATE LNAME LSTATEVAR LFUNCTION LCONST
%token LINIT LB1 LB2

%token LDIST_NORMAL LDIST_GAMMA LDIST_HYPEREXP       
       
%token LEXPOSE LHIDE LUNHIDE
%token LRUN 
 
%right LAND 
%right LOR 
%right LNOT
%right LPARALLEL LBPARALLEL 
%right LCHOICE 
%right LPCLOSE 
%right LDOT LCOMMA LDOTCOMMA
%right LBANG

%token maxParseToken 
%token LEX_WHITE LEX_COMMENT LEX_ERROR

%type <tree_node> dec_temp_elem
%type <tree_node> dec_temp_list

%left LPLUS LMINUS
%left LTIMES LDIV
%left NEG POS    /* unary minus and plus */
%right LEXP LLOG LPOW LSQRT

%start blenxfile

%%


blenxfile : 
     program            { System.Diagnostics.Trace.WriteLine("Parsing a prog file"); }
   | function           { System.Diagnostics.Trace.WriteLine("Parsing a func file"); }
   | types              { System.Diagnostics.Trace.WriteLine("Parsing a types file"); }
   ;
   
//COMMON

number : 
     LREAL                                                           
   | LDECIMAL                                                      
   ;
   
rate :
     number                                                          
   | LRATE LPOPEN LID LPCLOSE                                          {             
                                                                          //TODO: check if defined constant? Code completion?
                                                                          StartName(ToTextSpan(@1), "rate");
                                                                          StartParameters(ToTextSpan(@2));
                                                                          EndParameters(ToTextSpan(@4));
                                                                          Match(@2, @4); 
                                                                       }  
   | LRATE LPOPEN error                                                {    
                                                                          StartName(ToTextSpan(@1), "rate");
                                                                          System.Diagnostics.Trace.WriteLine("rate");
                                                                          StartParameters(ToTextSpan(@2));
                                                                          //CallErrorHandler("unmatched parentheses", @3); 
                                                                       }     
   | LINF                                                            
   ;
   
hypexp_parameter_list:
     LPOPEN number LCOMMA number LPCLOSE                               { Match(@1, @5); }
   | LPOPEN number LCOMMA number LPCLOSE LCOMMA hypexp_parameter_list  { Match(@1, @5); }
   ;
   

//PROGRAM
program : 
     info LDAOPEN rate_dec LDACLOSE dec_list LRUN bp                   { AddOutliningRegion(@2, @4); Match(@2, @4); }                   
   | info dec_list LRUN bp                                              
   | LDAOPEN rate_dec LDACLOSE dec_list LRUN bp                         
   | dec_list LRUN bp                                                   
   ;

info:
     LSOPEN LSTEPS LEQUAL LDECIMAL LSCLOSE                             { Match(@1, @5); }
   | LSOPEN LSTEPS LEQUAL LDECIMAL LCOMMA LDELTA LEQUAL number LSCLOSE { Match(@1, @9); }
   | LSOPEN LTIME LEQUAL number LSCLOSE                                { Match(@1, @5); }
   ;

betaid :
	  LID																		
   | LBNIL                                                     
   ;

rate_dec :
     LID LDDOT rate															
   | LRCHANGE LDDOT rate													
   | LREXPOSE LDDOT rate													
   | LRUNHIDE LDDOT rate													
   | LRHIDE LDDOT rate														
   | LBASERATE LDDOT rate													
   | rate_dec LCOMMA rate_dec												
   ;

dec_list: 
     dec                                                       
   | dec error
   | dec dec_list
   ;
		
//betaprocess :
// 	binder LSOPEN par LSCLOSE		                                { Match(@2, @4); }							
// |	binder LSOPEN sum LSCLOSE									           { Match(@2, @4); }
// ;		
		
dec :
     LLET LID LDDOT LPIPROCESS LEQUAL error                     { st.AddPProc(@2, this.currentFile, $2, @5, @6); }
   | LLET LID LDDOT LPIPROCESS LEQUAL par LDOTCOMMA             { AddOutliningRegion(@5, @7); st.AddPProc(@2, this.currentFile, $2, @5, @7); }
   | LLET LID LDDOT LPIPROCESS LEQUAL sum LDOTCOMMA             { AddOutliningRegion(@5, @7); st.AddPProc(@2, this.currentFile, $2, @5, @7); }
   | LLET LID LDDOT LBETAPROCESS LEQUAL LBB LPOPEN error        {                                                                   
                                                                   StartName(ToTextSpan(@6), "#");
                                                                   StartParameters(ToTextSpan(@7));
                                                                }
   | LLET LID LDDOT LBETAPROCESS LEQUAL LBB LPOPEN LID error    { 
                                                                   st.AddBProc(@2, this.currentFile, $2, new TextSpan { iStartLine = @6.sLin, iStartIndex = @6.sCol, iEndLine = @9.eLin, iEndIndex = @9.eCol }); 
                                                                } 
   | LLET LID LDDOT LBETAPROCESS LEQUAL LBBH LPOPEN error       {                                                                   
                                                                   StartName(ToTextSpan(@6), "#");
                                                                   StartParameters(ToTextSpan(@7));
                                                                }  
   | LLET LID LDDOT LBETAPROCESS LEQUAL LBBH LPOPEN LID error   { st.AddBProc(@2, this.currentFile, $2, new TextSpan { iStartLine = @6.sLin, iStartIndex = @6.sCol, iEndLine = @9.eLin, iEndIndex = @9.eCol }); }   
   | LLET LID LDDOT LBETAPROCESS LEQUAL binder LSOPEN error     { st.AddBProc(@2, this.currentFile, $2, ToTextSpan(@6), @7, @8); }   
   | LLET LID LDDOT LBETAPROCESS LEQUAL binder LSOPEN par LSCLOSE LDOTCOMMA   { AddOutliningRegion(@5, @10); st.AddBProc(@2, this.currentFile, $2, ToTextSpan(@6), @7, @9); Match(@7, @9);}   
   | LLET LID LDDOT LBETAPROCESS LEQUAL binder LSOPEN sum LSCLOSE LDOTCOMMA   { AddOutliningRegion(@5, @10); st.AddBProc(@2, this.currentFile, $2, ToTextSpan(@6), @7, @9); Match(@7, @9);}   
   | LLET LID LDDOT LBETAPROCESS LEQUAL 
     inv_temp_elem_params LDOTCOMMA                             { AddOutliningRegion(@5, @7); st.AddBProc(@2, this.currentFile, $2, null, @5, @7); }
   | LLET LID LDDOT LMOLECULE LEQUAL molecule LDOTCOMMA         { AddOutliningRegion(@5, @7); st.AddMolecule(@2, this.currentFile, $2, @7); }
   | LLET LID LDDOT LPREFIX LEQUAL error                        { st.AddSequence(@2, this.currentFile, $2, @6); }
   | LLET LID LDDOT LPREFIX LEQUAL dec_sequence LDOTCOMMA       { AddOutliningRegion(@5, @7); st.AddSequence(@2, this.currentFile, $2, @7); }
   | event_start verb LDOTCOMMA                                 { st.AddEvent(@1, this.currentFile, @3); }
   | LTEMPLATE LID LDDOT LPIPROCESS LDAOPEN dec_temp_list 
     LDACLOSE LEQUAL par LDOTCOMMA                              { AddOutliningRegion(@8, @10); Match(@5, @7); st.AddTemplatePProc(@2, this.currentFile, $2, (PTN_Dec_Temp_List)$6, @8, @10); }
   | LTEMPLATE LID LDDOT LPIPROCESS LDAOPEN dec_temp_list 
     LDACLOSE LEQUAL error                                      { Match(@5, @7); st.AddTemplatePProc(@2, this.currentFile, $2, (PTN_Dec_Temp_List)$6, @8, @9); }
   | LTEMPLATE LID LDDOT LPIPROCESS LDAOPEN dec_temp_list 
     LDACLOSE LEQUAL sum LDOTCOMMA                              { AddOutliningRegion(@8, @10); Match(@5, @7); st.AddTemplatePProc(@2, this.currentFile, $2, (PTN_Dec_Temp_List)$6, @8, @10); } 
   | LTEMPLATE LID LDDOT LBETAPROCESS LDAOPEN dec_temp_list 
     LDACLOSE LEQUAL LSOPEN par LSCLOSE LDOTCOMMA               { AddOutliningRegion(@8, @12); Match(@5, @7); st.AddTemplateBProc(@2, this.currentFile, $2, (PTN_Dec_Temp_List)$6, @9, @11); Match(@9, @11);} 
   | LTEMPLATE LID LDDOT LBETAPROCESS LDAOPEN dec_temp_list 
     LDACLOSE LEQUAL LSOPEN sum LSCLOSE LDOTCOMMA               { AddOutliningRegion(@8, @12); Match(@5, @7); st.AddTemplateBProc(@2, this.currentFile, $2, (PTN_Dec_Temp_List)$6, @9, @11); Match(@9, @11);} 
   | LTEMPLATE LID LDDOT LBETAPROCESS LDAOPEN dec_temp_list 
     LDACLOSE LEQUAL LSOPEN error                               { Match(@5, @7); st.AddTemplateBProc(@2, this.currentFile, $2, (PTN_Dec_Temp_List)$6, @9, @10); } 
   | LTEMPLATE LID LDDOT LBETAPROCESS LDAOPEN dec_temp_list 
     LDACLOSE LEQUAL binder LSOPEN error                        { Match(@5, @7); st.AddTemplateBProc(@2, this.currentFile, $2, (PTN_Dec_Temp_List)$6, @10, @11); }   
   | LTEMPLATE LID LDDOT LBETAPROCESS LDAOPEN dec_temp_list 
     LDACLOSE LEQUAL binder LSOPEN par LSCLOSE LDOTCOMMA        { AddOutliningRegion(@8, @13); Match(@5, @7); st.AddTemplateBProc(@2, this.currentFile, $2, (PTN_Dec_Temp_List)$6, @10, @12); Match(@10, @12);}   
   | LTEMPLATE LID LDDOT LBETAPROCESS LDAOPEN dec_temp_list 
     LDACLOSE LEQUAL binder LSOPEN sum LSCLOSE LDOTCOMMA        { AddOutliningRegion(@8, @13); Match(@5, @7); st.AddTemplateBProc(@2, this.currentFile, $2, (PTN_Dec_Temp_List)$6, @10, @12); Match(@10, @12);}
   ;
   
dec_temp_elem :
     LNAME LID                                                  { $$ = new PTN_Dec_Temp_Elem(new Pos(@$.sLin, @$.sCol, this.currentFile), TempType.TEMP_NAME, $2); }
   | LPIPROCESS LID                                             { $$ = new PTN_Dec_Temp_Elem(new Pos(@$.sLin, @$.sCol, this.currentFile), TempType.TEMP_PPROC, $2); }
   | LTYPE LID                                                  { $$ = new PTN_Dec_Temp_Elem(new Pos(@$.sLin, @$.sCol, this.currentFile), TempType.TEMP_TYPE, $2); }
   | LPREFIX LID                                                { $$ = new PTN_Dec_Temp_Elem(new Pos(@$.sLin, @$.sCol, this.currentFile), TempType.TEMP_SEQ, $2); } 
   | LRATE LID                                                  { $$ = new PTN_Dec_Temp_Elem(new Pos(@$.sLin, @$.sCol, this.currentFile), TempType.TEMP_RATE, $2); } 
   ;
	
dec_temp_list :
		dec_temp_elem										{ $$ = new PTN_Dec_Temp_List(new Pos(@$.sLin, @$.sCol, this.currentFile),$1); }
	|	dec_temp_elem LCOMMA dec_temp_list        { $$ = new PTN_Dec_Temp_List(new Pos(@$.sLin, @$.sCol, this.currentFile),$1,$3); }
	;
	
inv_temp_elem_params :
     LID LDAOPEN error                          { StartName(ToTextSpan(@1), $1); StartParameters(ToTextSpan(@2)); }
   | LID LDAOPEN inv_temp_list error            { StartName(ToTextSpan(@1), $1); StartParameters(ToTextSpan(@2)); }
   | LID LDAOPEN inv_temp_list LDACLOSE			{ StartName(ToTextSpan(@1), $1); StartParameters(ToTextSpan(@2)); EndParameters(ToTextSpan(@4)); Match(@2, @4); }
   ;
	
inv_temp_elem :			
	  LID														
   | LREAL 
   | inv_temp_elem_params                                     
   | LPOPEN LID LCOMMA LSTATEUNHIDE LPCLOSE     { Match(@1, @5); }
   | LPOPEN LID LCOMMA LSTATEHIDE LPCLOSE       { Match(@1, @5); }
   ;
	
inv_temp_list :
      inv_temp_elem                             
   |  inv_temp_list LCOMMA inv_temp_elem        { NextParameter(ToTextSpan(@2)); }
   |  inv_temp_list LCOMMA error                { NextParameter(ToTextSpan(@2)); }
   ;

//for declaration of a sequence of actions NOT ended by a pi process:
//this kind of sequence is NOT a pi-process, but it is useful for putting them in templates with
//"pieces" of sequences in common	
dec_sequence:
     action                                                    
   | action LDOT dec_sequence                                  
   ;
  
state_op:
     LID LLEFTARROW number                                     
   | LID LRIGHTARROW number                                    
   ;
   
state_op_list:
     state_op                                                  
   | state_op LCOMMA state_op_list                             
   ;
   
entity:
     LID                                                       
   ;
   
entity_list:
     entity                                               
   | entity LCOMMA entity_list                                
   ;
   
cond_atom:
     LPARALLEL LID LPARALLEL LEQUAL LDECIMAL			 { Match(@1, @3); }
   | LPARALLEL LID LPARALLEL LACLOSE LDECIMAL		 { Match(@1, @3); }
   | LPARALLEL LID LPARALLEL LAOPEN LDECIMAL			 { Match(@1, @3); }
   | LPARALLEL LID LPARALLEL LNEQUAL LDECIMAL		 { Match(@1, @3); }
   | LTIME LEQUAL LREAL										
   | LSTEP LEQUAL LDECIMAL									
   | state_op_list											
   ;
   
cond_expression :
     cond_atom												
   | cond_expression LAND cond_expression			
   | cond_expression LOR cond_expression			
   | LNOT cond_expression								
   | LPOPEN cond_expression LPCLOSE				                                                   { Match(@1, @3); }	
   ;

event_start :
     LWHEN LPOPEN error                                                         { StartName(ToTextSpan(@1), "Event"); StartParameters(ToTextSpan(@2)); }
   | LWHEN LPOPEN entity_list LDDOT error                                       { 
                                                                                   StartName(ToTextSpan(@1), "Event"); 
                                                                                   StartParameters(ToTextSpan(@2)); 
                                                                                   NextParameter(ToTextSpan(@4));
                                                                                }

                                                                                                             
   | LWHEN LPOPEN entity_list LDDOT cond_expression LDDOT rate LPCLOSE          {  
                                                                                   StartName(ToTextSpan(@1), "Event"); 
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   NextParameter(ToTextSpan(@4)); 
                                                                                   NextParameter(ToTextSpan(@6));
                                                                                   EndParameters(ToTextSpan(@8)); 
                                                                                   Match(@2, @8);
                                                                                }       
   | LWHEN LPOPEN entity_list LDDOT cond_expression LDDOT LID LPCLOSE           {  
                                                                                   StartName(ToTextSpan(@1), "Event"); 
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   NextParameter(ToTextSpan(@4)); 
                                                                                   NextParameter(ToTextSpan(@6));
                                                                                   EndParameters(ToTextSpan(@8)); 
                                                                                   Match(@2, @8);
                                                                                }         
   | LWHEN LPOPEN entity_list LDDOT cond_expression LDDOT LPCLOSE               {  
                                                                                   StartName(ToTextSpan(@1), "Event"); 
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   NextParameter(ToTextSpan(@4)); 
                                                                                   NextParameter(ToTextSpan(@6));
                                                                                   EndParameters(ToTextSpan(@7)); 
                                                                                   Match(@2, @7);
                                                                                }       
   | LWHEN LPOPEN entity_list LDDOT cond_expression LDDOT error                 {  
                                                                                   StartName(ToTextSpan(@1), "Event"); 
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   NextParameter(ToTextSpan(@4)); 
                                                                                   NextParameter(ToTextSpan(@6)); 
                                                                                }      
   | LWHEN LPOPEN entity_list LDDOT cond_expression LDDOT 
     LDIST_NORMAL LPOPEN number LCOMMA number LPCLOSE                           {  
                                                                                   StartName(ToTextSpan(@1), "Event"); 
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   Match(@6, @10); 
                                                                                   NextParameter(ToTextSpan(@2)); 
                                                                                   NextParameter(ToTextSpan(@4));
                                                                                }
   | LWHEN LPOPEN entity_list LDDOT cond_expression LDDOT 
     LDIST_GAMMA LPOPEN number LCOMMA number LPCLOSE	                          {  
                                                                                   StartName(ToTextSpan(@1), "Event"); 
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   Match(@6, @10); 
                                                                                   NextParameter(ToTextSpan(@2)); 
                                                                                   NextParameter(ToTextSpan(@4));
                                                                                }  
   | LWHEN LPOPEN entity_list LDDOT LDDOT rate LPCLOSE                          {  
                                                                                   StartName(ToTextSpan(@1), "Event");
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   NextParameter(ToTextSpan(@4)); 
                                                                                   NextParameter(ToTextSpan(@5)); 
                                                                                   EndParameters(ToTextSpan(@7)); 
                                                                                   Match(@2, @7);
                                                                                }     
   | LWHEN LPOPEN entity_list LDDOT LDDOT LID LPCLOSE                           {  
                                                                                   StartName(ToTextSpan(@1), "Event");
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   NextParameter(ToTextSpan(@4)); 
                                                                                   NextParameter(ToTextSpan(@5)); 
                                                                                   EndParameters(ToTextSpan(@7)); 
                                                                                   Match(@2, @7);
                                                                                }           
   | LWHEN LPOPEN entity_list LDDOT LDDOT error                                 {  
                                                                                   StartName(ToTextSpan(@1), "Event");
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   NextParameter(ToTextSpan(@4)); 
                                                                                   NextParameter(ToTextSpan(@5));                                                                                    
                                                                                }
   | LWHEN LPOPEN entity_list LDDOT LDDOT 
     LDIST_NORMAL LPOPEN number LCOMMA number LPCLOSE	 LPCLOSE                  {  
                                                                                   StartName(ToTextSpan(@1), "Event");
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   NextParameter(ToTextSpan(@4)); 
                                                                                   NextParameter(ToTextSpan(@5)); 
                                                                                   EndParameters(ToTextSpan(@12)); 
                                                                                   Match(@2, @12);
                                                                                   Match(@7, @11);
                                                                                }   
   | LWHEN LPOPEN entity_list LDDOT LDDOT 
     LDIST_GAMMA LPOPEN number LCOMMA number LPCLOSE LPCLOSE                    {  
                                                                                   StartName(ToTextSpan(@1), "Event");
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   NextParameter(ToTextSpan(@4)); 
                                                                                   NextParameter(ToTextSpan(@5)); 
                                                                                   EndParameters(ToTextSpan(@12)); 
                                                                                   Match(@2, @12);
                                                                                   Match(@7, @11);
                                                                                }  
   | LWHEN LPOPEN entity_list LDDOT LDDOT 
     LDIST_HYPEREXP LPOPEN hypexp_parameter_list LPCLOSE LPCLOSE	              {  
                                                                                   StartName(ToTextSpan(@1), "Event");
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   NextParameter(ToTextSpan(@4)); 
                                                                                   NextParameter(ToTextSpan(@5)); 
                                                                                   EndParameters(ToTextSpan(@10)); 
                                                                                   Match(@2, @10);
                                                                                   Match(@7, @9);
                                                                                }   
   | LWHEN LPOPEN LDDOT cond_expression LDDOT LPCLOSE                           {  
                                                                                   StartName(ToTextSpan(@1), "Event");
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   NextParameter(ToTextSpan(@3)); 
                                                                                   NextParameter(ToTextSpan(@5)); 
                                                                                   EndParameters(ToTextSpan(@6)); 
                                                                                   Match(@2, @6);
                                                                                }
   | LWHEN LPOPEN LDDOT cond_expression LDDOT error                             {  
                                                                                   StartName(ToTextSpan(@1), "Event");
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   NextParameter(ToTextSpan(@3)); 
                                                                                   NextParameter(ToTextSpan(@5));                                                                                    
                                                                                }       
   | LWHEN LPOPEN LDDOT error                                                   {  
                                                                                   StartName(ToTextSpan(@1), "Event");
                                                                                   StartParameters(ToTextSpan(@2));
                                                                                   NextParameter(ToTextSpan(@3));                                                                                                                                                                    
                                                                                } 
   ;
   

verb :
     LSPLIT LPOPEN betaid LCOMMA betaid LPCLOSE                                                 { Match(@2, @6); }
   | LNEW LPOPEN LDECIMAL LPCLOSE                                                               { Match(@2, @4); }
   | LDELETE LPOPEN LDECIMAL LPCLOSE                                                            { Match(@2, @4); }
   | LNEW                                                     
   | LDELETE                                                  
   | LJOIN LPOPEN betaid LPCLOSE                              
   | LJOIN                                                    
   | LUPDATE LPOPEN LID LCOMMA LID LPCLOSE                    
   ;

sumelem:
     LNIL                                      
   | seq                                       
   | LIF expression LTHEN sum LENDIF           
   ;

parelem:
	LID													
	| inv_temp_elem_params
//|	LID LDAOPEN inv_temp_list LDACLOSE			              { Match(@2, @4); }
|	LBANG action LDOT par							
|	LBANG action LDOT sum							
|	LIF expression LTHEN par LENDIF				
;

sum:
    sumelem                                    
   //|	sum LCHOICE sumelem                   
   | sum LCHOICE sum									
   | LPOPEN sum LPCLOSE								              { Match(@1, @3); }
   ;

par:
     parelem									
  | sum LPARALLEL sum						
  | sum LPARALLEL par						
  | par LPARALLEL sum					
  | par LPARALLEL par					 
  | LPOPEN par LPCLOSE					                       { Match(@1, @3); }
  ;
   
seq:
    action									
|	action LDOT par						
|	action LDOT sum						
|	LID LDOT par							
|	LID LDOT sum							
   ;
   
   
atom:
	LPOPEN LID LCOMMA LID LPCLOSE						           { Match(@1, @5); }			
|	LPOPEN LID LCOMMA LSTATEHIDE LPCLOSE						  { Match(@1, @5); }		
|	LPOPEN LID LCOMMA LSTATEUNHIDE LPCLOSE						  { Match(@1, @5); }		
|	LPOPEN LID LCOMMA LSTATEBOUND LPCLOSE						  { Match(@1, @5); }		
|	LPOPEN LID LCOMMA LID LCOMMA LSTATEHIDE LPCLOSE			  { Match(@1, @7); }		
|	LPOPEN LID LCOMMA LID LCOMMA LSTATEUNHIDE LPCLOSE		  { Match(@1, @7); }
|	LPOPEN LID LCOMMA LID LCOMMA LSTATEBOUND LPCLOSE		  { Match(@1, @7); }
;

expression:
	atom																
|   expression LAND expression									
|   expression LOR expression										
|   LNOT expression													
|   LPOPEN expression LPCLOSE						              { Match(@1, @3); }				
;

action :
	LTAU LPOPEN rate LPCLOSE						                    { Match(@2, @4); }				
|	LID LEM LPOPEN LID LPCLOSE									        	  { 
                                                                    StartName(ToTextSpan(@1), $1); 
                                                                    StartParameters(ToTextSpan(@3));
                                                                    EndParameters(ToTextSpan(@5));
                                                                    Match(@3, @5);
                                                                 }
|	LID LEM LPOPEN error									        	        { 
                                                                    StartName(ToTextSpan(@1), $1); 
                                                                    StartParameters(ToTextSpan(@3));                                                                    
                                                                 }		
|	LID LEM LPOPEN LPCLOSE                                       { Match(@3, @4); }
|	LID LQM LPOPEN LPCLOSE                                       { Match(@3, @4); }
|	LID LQM LPOPEN LID LPCLOSE                                   { 
                                                                   StartName(ToTextSpan(@1), $1); 
                                                                   StartParameters(ToTextSpan(@3));
                                                                   EndParameters(ToTextSpan(@5));
                                                                   Match(@3, @5); 
                                                                 }
|	LID LQM LPOPEN error                                         { 
                                                                    StartName(ToTextSpan(@1), $1); 
                                                                    StartParameters(ToTextSpan(@3));                                                                    
                                                                 }
|	LEXPOSE LPOPEN LID LDDOT rate LCOMMA LID LPCLOSE             { Match(@2, @7); }
|	LEXPOSE LPOPEN rate LCOMMA LID LDDOT rate LCOMMA LID LPCLOSE { Match(@2, @10); }
| 	LHIDE LPOPEN error									         {
                                                                    StartName(ToTextSpan(@1), "hide"); 
                                                                    StartParameters(ToTextSpan(@2));                                                                    
                                                                 }
| 	LHIDE LPOPEN LID LPCLOSE                                     {
                                                                    StartName(ToTextSpan(@1), "hide"); 
                                                                    StartParameters(ToTextSpan(@2));
                                                                    EndParameters(ToTextSpan(@4));                                                                   
                                                                    Match(@2, @4); 
                                                                 }
| 	LHIDE LPOPEN rate LCOMMA error					             {
                                                                    StartName(ToTextSpan(@1), "hide"); 
                                                                    StartParameters(ToTextSpan(@2));
                                                                    NextParameter(ToTextSpan(@4));
                                                                 }                                                                 
| 	LHIDE LPOPEN rate LCOMMA LID LPCLOSE					     {
                                                                    StartName(ToTextSpan(@1), "hide"); 
                                                                    StartParameters(ToTextSpan(@2));
                                                                    NextParameter(ToTextSpan(@4));
                                                                    EndParameters(ToTextSpan(@6));                                                                   
                                                                    Match(@2, @6); 
                                                                 }
| 	LUNHIDE LPOPEN error                                         {
                                                                    StartName(ToTextSpan(@1), "hide"); 
                                                                    StartParameters(ToTextSpan(@2));                                                                    
                                                                 }
| 	LUNHIDE LPOPEN LID LPCLOSE                                   {
                                                                    StartName(ToTextSpan(@1), "hide"); 
                                                                    StartParameters(ToTextSpan(@2));
                                                                    EndParameters(ToTextSpan(@4));                                                                   
                                                                    Match(@2, @4); 
                                                                 }
| 	LUNHIDE LPOPEN rate LCOMMA error					         {
                                                                    StartName(ToTextSpan(@1), "hide"); 
                                                                    StartParameters(ToTextSpan(@2));
                                                                    NextParameter(ToTextSpan(@4));
                                                                 }                                                                 
| 	LUNHIDE LPOPEN rate LCOMMA LID LPCLOSE					     {
                                                                    StartName(ToTextSpan(@1), "hide"); 
                                                                    StartParameters(ToTextSpan(@2));
                                                                    NextParameter(ToTextSpan(@4));
                                                                    EndParameters(ToTextSpan(@6));                                                                   
                                                                    Match(@2, @6); 
                                                                 }
|	LCHANGE LPOPEN error                                         {
                                                                    StartName(ToTextSpan(@1), "ch"); 
                                                                    StartParameters(ToTextSpan(@2));                                                                    
                                                                 }				
|	LCHANGE LPOPEN rate LCOMMA error                             {
                                                                    StartName(ToTextSpan(@1), "ch"); 
                                                                    StartParameters(ToTextSpan(@2));
                                                                    NextParameter(ToTextSpan(@4));                                                                    
                                                                 }
|	LCHANGE LPOPEN rate LCOMMA LID LCOMMA error                  {
                                                                    StartName(ToTextSpan(@1), "ch"); 
                                                                    StartParameters(ToTextSpan(@2));
                                                                    NextParameter(ToTextSpan(@4)); 
                                                                    NextParameter(ToTextSpan(@6));                                                                   
                                                                 }
|	LCHANGE LPOPEN LID LCOMMA error                              {
                                                                    StartName(ToTextSpan(@1), "ch"); 
                                                                    StartParameters(ToTextSpan(@2));
                                                                    NextParameter(ToTextSpan(@4)); 
                                                                 }                                                                
|	LCHANGE LPOPEN rate LCOMMA LID LCOMMA LID LPCLOSE            {    
                                                                    StartName(ToTextSpan(@1), "ch"); 
                                                                    StartParameters(ToTextSpan(@2));
                                                                    NextParameter(ToTextSpan(@4));
                                                                    NextParameter(ToTextSpan(@6));
                                                                    EndParameters(ToTextSpan(@8));                                                                   
                                                                    Match(@2, @8); 
                                                                 }
|	LCHANGE LPOPEN LID LCOMMA LID LPCLOSE                        {    
                                                                    StartName(ToTextSpan(@1), "ch"); 
                                                                    StartParameters(ToTextSpan(@2));
                                                                    NextParameter(ToTextSpan(@4));                                                                    
                                                                    EndParameters(ToTextSpan(@6));                                                                   
                                                                    Match(@2, @6); 
                                                                 }
|	LDIE
|	LDIE LPOPEN error        			                         {    
                                                                    StartName(ToTextSpan(@1), "die"); 
                                                                    StartParameters(ToTextSpan(@2));                                                                    
                                                                 }																
|	LDIE LPOPEN rate LPCLOSE			                         {    
                                                                    StartName(ToTextSpan(@1), "die"); 
                                                                    StartParameters(ToTextSpan(@2));
                                                                    EndParameters(ToTextSpan(@4));                                                                   
                                                                    Match(@2, @4); 
                                                                 }						
;



binder :
     LBB LPOPEN LID LDDOT rate LCOMMA LID LPCLOSE     { 
                                                         StartName(ToTextSpan(@1), "#"); 
                                                         StartParameters(ToTextSpan(@2));
                                                         NextParameter(ToTextSpan(@4));
                                                         NextParameter(ToTextSpan(@6));
                                                         EndParameters(ToTextSpan(@8));                                                                   
                                                         Match(@2, @8); 
                                                         st.AddBinder(@3, this.currentFile, $3, $7);
                                                      }
   | LBB LPOPEN error                                 { 
                                                         StartName(ToTextSpan(@1), "#"); 
                                                         StartParameters(ToTextSpan(@2));                                                         
                                                      }
   | LBB LPOPEN LID LDDOT error                       { 
                                                         StartName(ToTextSpan(@1), "#"); 
                                                         StartParameters(ToTextSpan(@2));
                                                         NextParameter(ToTextSpan(@4));                                                          
                                                      }  
   | LBB LPOPEN LID LDDOT rate LCOMMA error           { 
                                                         StartName(ToTextSpan(@1), "#"); 
                                                         StartParameters(ToTextSpan(@2));
                                                         NextParameter(ToTextSpan(@4));
                                                         NextParameter(ToTextSpan(@6));                                                         
                                                      }                                                
   | LBBH LPOPEN LID LDDOT rate LCOMMA LID LPCLOSE    { 
                                                         StartName(ToTextSpan(@1), "#"); 
                                                         StartParameters(ToTextSpan(@2));
                                                         NextParameter(ToTextSpan(@4));
                                                         NextParameter(ToTextSpan(@6));
                                                         EndParameters(ToTextSpan(@8));                                                                   
                                                         Match(@2, @8); 
                                                         st.AddBinder(@3, this.currentFile, $3, $7);
                                                      }
   | LBBH LPOPEN error                                { 
                                                         StartName(ToTextSpan(@1), "#"); 
                                                         StartParameters(ToTextSpan(@2));                                                         
                                                      }
   | LBBH LPOPEN LID LDDOT error                      { 
                                                         StartName(ToTextSpan(@1), "#"); 
                                                         StartParameters(ToTextSpan(@2));
                                                         NextParameter(ToTextSpan(@4));                                                          
                                                      }  
   | LBBH LPOPEN LID LDDOT rate LCOMMA error          { 
                                                         StartName(ToTextSpan(@1), "#"); 
                                                         StartParameters(ToTextSpan(@2));
                                                         NextParameter(ToTextSpan(@4));
                                                         NextParameter(ToTextSpan(@6));                                                         
                                                      }                    
   | LBB LPOPEN LID LCOMMA LID LPCLOSE                { 
                                                         StartName(ToTextSpan(@1), "#"); 
                                                         StartParameters(ToTextSpan(@2));
                                                         NextParameter(ToTextSpan(@4));
                                                         EndParameters(ToTextSpan(@6));                                                                   
                                                         Match(@2, @6); 
                                                         st.AddBinder(@3, this.currentFile, $3, $5);
                                                      }
   | LBB LPOPEN LID LCOMMA error                      { 
                                                         StartName(ToTextSpan(@1), "#"); 
                                                         StartParameters(ToTextSpan(@2));
                                                         NextParameter(ToTextSpan(@4));                                                          
                                                      }  
   | LBBH LPOPEN LID LCOMMA LID LPCLOSE               { 
                                                         StartName(ToTextSpan(@1), "#"); 
                                                         StartParameters(ToTextSpan(@2));
                                                         NextParameter(ToTextSpan(@4));
                                                         EndParameters(ToTextSpan(@6));                                                                   
                                                         Match(@2, @6); 
                                                         st.AddBinder(@3, this.currentFile, $3, $5);
                                                      }
   | LBBH LPOPEN LID LCOMMA error                     { 
                                                         StartName(ToTextSpan(@1), "#"); 
                                                         StartParameters(ToTextSpan(@2));
                                                         NextParameter(ToTextSpan(@4));                                                          
                                                      }              
   | binder LCOMMA binder											
   ;


bp:
	LDECIMAL LID													
|	LID LID                                           
|	LDECIMAL inv_temp_elem_params //LID LDAOPEN inv_temp_list LDACLOSE        
|	bp LBPARALLEL bp												
;

molecule :
     LGOPEN mol_signature LDOTCOMMA node_list LGCLOSE 
   ;
  

mol_signature : 
     LPOPEN edge_list LPCLOSE                         
   ;
   
edge_list : 
     edge                                             
   | edge LCOMMA edge_list                            
   ; 

edge : 
     LPOPEN LID LCOMMA LID LCOMMA LID LCOMMA LID LPCLOSE    
   ;
   
node_list :
     node                                                            
   | node node_list                                                  
   ;

node : 
     LID LDDOT LID LEQUAL LPOPEN mol_binder_list LPCLOSE LDOTCOMMA   
   | LID LEQUAL LID LDOTCOMMA                                        
   ;
   
mol_binder : 
     LID																
   ;
     
mol_binder_list :
     mol_binder													
   | mol_binder LCOMMA mol_binder_list						
   ;
   
//FUNC

function : 
     fun_dec_list
   ;

fun_dec_list: 
     fun_dec                                                       
   | fun_dec error
   | fun_dec fun_dec_list
   ;

fun_dec :
     LLET LID LDDOT LFUNCTION LEQUAL exp LDOTCOMMA                       { st.AddFunction(@2, this.currentFile, $2, @7); }
   | LLET LID LDDOT LSTATEVAR LEQUAL exp LDOTCOMMA                       { st.AddVar(@2, this.currentFile, $2, @7); }
   | LLET LID LPOPEN number LPCLOSE LDDOT LSTATEVAR LEQUAL exp LDOTCOMMA { st.AddVar(@2, this.currentFile, $2, @10); Match(@3, @5); }
   | LLET LID LPOPEN number LPCLOSE LDDOT LSTATEVAR LEQUAL exp 
             LINIT number LDOTCOMMA                                      { st.AddVar(@2, this.currentFile, $2, @12); Match(@3, @5); }   
   | LLET LID LDDOT LCONST LEQUAL exp LDOTCOMMA                          { st.AddConstant(@2, this.currentFile, $2, @7); }
   ;
   
exp:
     number                                                    {  }
   | LID                                                       {  }
   | LPARALLEL LB1 LPARALLEL                                   { Match(@1, @3); }
   | LPARALLEL LB2 LPARALLEL                                   { Match(@1, @3); }
   | LPARALLEL LID LPARALLEL                                   { Match(@1, @3); }
   | LLOG LPOPEN exp LPCLOSE                                   { Match(@2, @4); }
   | LSQRT LPOPEN exp LPCLOSE                                  { Match(@2, @4); }
   | LEXP LPOPEN exp LPCLOSE                                   { Match(@2, @4); }
   | LPOW LPOPEN exp LCOMMA exp LPCLOSE                        { Match(@2, @6); }
   | exp LPLUS exp                                             {  }
   | exp LMINUS exp                                            {  }
   | exp LTIMES exp                                            {  }
   | exp LDIV exp                                              {  }
   | LMINUS exp      %prec NEG                                 {  }
   | LPLUS exp       %prec POS                                 {  }
   | LPOPEN exp LPCLOSE                                        { Match(@1, @3); }
   ;
   
//TYPE
types : 
     LGOPEN type_list LGCLOSE                                        { AddOutliningRegion(@1, @3); Match(@1, @3); }
   | LGOPEN type_list LGCLOSE LDELIM LGOPEN affinity_list LGCLOSE    { AddOutliningRegion(@1, @3); AddOutliningRegion(@5, @7); Match(@1, @3); Match(@5, @7); }
   ;

type_list:																
     LID                                                             { st.AddBinderIdentifier(@1, this.currentFile, $1); }
   | type_list LCOMMA LID                                            { st.AddBinderIdentifier(@3, this.currentFile, $3); }
   ;

affinity:
     LPOPEN LID LCOMMA LID LCOMMA rate LPCLOSE                                                { st.AddAffinity(@2, this.currentFile, $2, $4); Match(@1, @7); }
   | LPOPEN LID LCOMMA LID LCOMMA LID LPCLOSE                                                 { st.AddAffinity(@2, this.currentFile, $2, $4); Match(@1, @7); }
   | LPOPEN LID LCOMMA LID LCOMMA rate LCOMMA rate LCOMMA rate LPCLOSE                        { st.AddAffinity(@2, this.currentFile, $2, $4); Match(@1, @11); }
   | LPOPEN LID LCOMMA LID LCOMMA LDIST_NORMAL LPOPEN number LCOMMA number LPCLOSE LPCLOSE    { st.AddAffinity(@2, this.currentFile, $2, $4); Match(@7, @11); Match(@1, @12); }
   | LPOPEN LID LCOMMA LID LCOMMA LDIST_GAMMA LPOPEN number LCOMMA number LPCLOSE LPCLOSE     { st.AddAffinity(@2, this.currentFile, $2, $4); Match(@7, @11); Match(@1, @12); }
   | LPOPEN LID LCOMMA LID LCOMMA LDIST_HYPEREXP LPOPEN hypexp_parameter_list LPCLOSE LPCLOSE { st.AddAffinity(@2, this.currentFile, $2, $4); Match(@7, @9); Match(@1, @10); }
   ; 	          
	
affinity_list:
     affinity                           
   | affinity LCOMMA affinity_list   
   ;  
		
%%
