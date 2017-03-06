using System;

namespace BlankFiller.Models
{
    public class TextBlock : ICloneable
    {
        public TextBlock(string text, int x, int y)
        {
            Text = text ?? "";
            X = x;
            Y = y;
        }

        public string Text { get; private set; }

        public int X { get; private set; }

        public int Y { get; private set; }

        public object Clone()
        {
            return new TextBlock(Text, X, Y);
        }

        public override string ToString()
        {
            return $"{Text} ({X},{Y})";            
        }
    }
}
