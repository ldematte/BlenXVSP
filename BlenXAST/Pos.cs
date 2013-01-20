using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dema.BlenX.Parser
{
    public struct Pos
    {
        public string Filename;
        public int Line;
        public int Column;

        public static Pos Empty = new Pos(0, 0, "");

        public Pos(int Line, int Column, string Filename)
        {
            this.Filename = Filename;
            this.Line = Line;
            this.Column = Column;
        }
    }
}
