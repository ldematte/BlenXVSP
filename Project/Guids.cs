using System;

namespace Dema.BlenX.VisualStudio.Project
{
    static class GuidList
    {
        public const string guidProjectPkgString = "f7ef0100-dce5-47c3-a44f-fab4eac4cbed";
        public const string guidProjectCmdSetString = "44d0799f-f1c8-4363-8e73-be92ed45a64c";
        public const string guidBaseProjectFactoryString = "5A5CBCB8-1EB8-4bdc-8EA0-6CD8519516FF";

        public const string guidGeneralOptionsString = "69198A38-A8FB-4f19-9A1B-578D8EC4167D";

        public static readonly Guid guidGeneralOptions = new Guid(guidGeneralOptionsString);
        public static readonly Guid guidProjectCmdSet = new Guid(guidProjectCmdSetString);
        public static readonly Guid guidBaseProjectFactory = new Guid(guidBaseProjectFactoryString);
    };
}