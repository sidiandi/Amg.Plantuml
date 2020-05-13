using Amg.Extensions;
using Amg.FileSystem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Amg.Plantuml
{
    [TestFixture]
    public sealed class LocalTests
    {
        string TestDir = typeof(LocalTests).GetProgramDataDirectory();

        [Test]
        public async Task Test()
        {
            var count = 10;

            var plantumlMarkup = @"@startuml
Alice -> Bob: Authentication Request
Bob --> Alice: Authentication Response

Alice -> Bob: Another authentication Request
Alice <-- Bob: another authentication Response
@enduml
";

            var sw = Stopwatch.StartNew();
            using (var plantuml = Plantuml.Local(new LocalSettings
            {
                Options = new[] { "-v" }
            }))
            {
                for (int i = 0; i < count; ++i)
                {
                    var outputFile = TestDir.Combine($@"Alice-{i}.png");
                    await plantuml.Convert(plantumlMarkup, outputFile);
                    AssertIsValidPng(outputFile);
                }
            }
            Console.WriteLine(sw.Elapsed);
        }

        [Test]
        public async Task Test2()
        {
            var count = 10;

            var plantumlMarkup = @"@startuml

actor Utilisateur as user
participant ""formSign.js"" as form <<Contrôleur formulaire>>
participant ""Sign.java"" as controler <<(C,#ADD1B2) Contrôleur formulaire>>
participant ""Secure.java"" as secure <<(C,#ADD1B2) authentification>>
participant ""Security.java"" as security <<(C,#ADD1B2) sécurité>>

box ""Application Web"" #LightBlue
 participant form
end box

box ""Serveur Play"" #LightGreen
 participant controler
 participant secure
 participant security
end box

user -> form : submitSignIn()
form -> form : getParameters()
form -> form : result = checkFields()

alt result

    form -> controler : formSignIn(email,pwd)
    controler -> controler : result = checkFields()
    
    alt result
     controler -> secure : Secure.authenticate(email, pwd, true);
     secure -> security : onAuthenticated()
     security --> form : renderJSON(0);
     form --> user : display main page
    else !result
     controler --> form : renderJSON(1)
     form --> user : display error
    end
    
else !result
 form --> user : display error
end

@enduml
";

            var sw = Stopwatch.StartNew();
            using (var plantuml = Plantuml.Local(new LocalSettings
            {
                Options = new[] { "-v" }
            }))
            {
                for (int i = 0; i < count; ++i)
                {
                    var outputFile = TestDir.Combine($@"Alice-{i}.png");
                    await plantuml.Convert(plantumlMarkup, outputFile);
                    AssertIsValidPng(outputFile);
                }
            }
            Console.WriteLine(sw.Elapsed);
        }

        static void AssertIsValidPng(string outputFile)
        {
            Assert.That(new FileInfo(outputFile).Length > 0);
            using (var image = System.Drawing.Image.FromFile(outputFile))
            {
            }
        }

        [Test]
        public async Task ParallelInput()
        {
            var plantumlMarkup = @"@startuml
Alice -> Bob: Authentication Request
Bob --> Alice: Authentication Response

Alice -> Bob: Another authentication Request
Alice <-- Bob: another authentication Response
@enduml
";

            var outFile = TestDir.Combine("out.png");
            using (var plantuml = Plantuml.Local())
            {
                var results = Enumerable.Range(0, 20).AsParallel()
                    .Select(_ =>
                    {
                        plantuml.Convert(plantumlMarkup, outFile);
                        return true;
                    })
                    .ToList();
                Assert.That(results.All(_ => _));
            };
        }

        [Test]
        public async Task Download()
        {
            var local = new Local(new LocalSettings());
            await local.DownloadGraphviz();
            await local.DownloadPlantuml();
        }

        [Test]
        public async Task Tool()
        {
            var local = new Local(new LocalSettings());
            var r = await local.Tool.Run("-testdot");
        }
    }
}
