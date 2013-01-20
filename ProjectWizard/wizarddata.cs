namespace Dema.BlenX
{
    using System;
    
    /// <summary>
    /// Data that is collected by the wizard
    /// </summary>
    public class WizardData
    {
        string baseFileName;
        string baseBoxName;
        bool addFuncFile;

        public bool AddFuncFile
        {
           get { return addFuncFile; }
           set { addFuncFile = value; }
        }

        public string BaseFileName
        {
            get { return this.baseFileName; }
            set { this.baseFileName = value; }
        }

        public string BaseBoxName
        {
            get { return this.baseBoxName; }
            set { this.baseBoxName = value; }
        }
    }
}
