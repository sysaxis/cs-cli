using System;
using System.Collections.Generic;
using System.Text;

using CLI;

namespace CLISample
{
    class CalculatorCommands
    {
        private float Result { get; set; } = 0f;

        public CalculatorCommands()
        {
            new Command
            {
                Name = "calc reset",
                Handler = a => Result = 0f
            };

            new Command
            {
                Name = "calc",
                Handler = a =>
                {
                    float? add = a.Get<float>("add");
                    float? sub = a.Get<float>("sub");

                    if (add.HasValue)
                    {
                        Result += add.Value;
                    }
                    if (sub.HasValue)
                    {
                        Result += sub.Value;
                    }

                    a.SetResult("result", Result);
                }
            };
        }
    }
}
