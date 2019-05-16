using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Reflection.Test
{
    class TestObject
    {
        public NestedTestObject NTO { get; set; }
    }
    class NestedTestObject
    {
        public string PropA { get; set; }
    }
}
