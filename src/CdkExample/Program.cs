using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CdkExample
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new App(null);
            new CdkExampleStack(app, "CdkExampleStack", new StackProps());
            app.Synth();
        }
    }
}
