
using System;
using System.Collections.Generic;
using System.IO;

namespace Dema.BlenX.VisualStudio.Project
{
    public class BetaSimResult
    {
        private string baseOutputName;

        public string BaseOutputName
        {
            get { return baseOutputName; }
            set { baseOutputName = value; }
        }
        private DateTime simulationTime;

        private String outputPath;

        public String OutputPath
        {
            get { return outputPath; }
            set { outputPath = value; }
        }

        public DateTime SimulationTime
        {
            get { return simulationTime; }
            set { simulationTime = value; }
        }        
    }

    public class BetaSimResults
    {
        private List<BetaSimResult> resultList = new List<BetaSimResult>();

        public List<BetaSimResult> ResultList
        {
            get { return resultList; }
            set { resultList = value; }
        }

        public static BetaSimResults LoadFromFile(string fileName)
        {
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(BetaSimResults));
            using (var stream = File.OpenRead(fileName))
            {
                return (BetaSimResults)x.Deserialize(stream);
            }
        }

        public static void SaveToFile(string fileName, BetaSimResults results)
        {
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(BetaSimResults));
            using (var stream = File.OpenWrite(fileName))
            {
                x.Serialize(stream, results);
            }
        }

    }
}
