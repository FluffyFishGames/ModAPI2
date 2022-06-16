using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using ModAPI.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using NLog;
using NLog.Config;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;

namespace ModAPI
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            /*var coreLibrary = AssemblyDefinition.ReadAssembly(@"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\6.0.1\System.Private.CoreLib.dll");
            var compilerGeneratedAttribute = coreLibrary.MainModule.GetType("System.Runtime.CompilerServices.CompilerGeneratedAttribute");
            MethodDefinition compilerGeneratedConstructor = null;
            foreach (var m in compilerGeneratedAttribute.Methods)
            {
                if (m.IsConstructor)
                    compilerGeneratedConstructor = m;
            }

            var testLibrary = AssemblyDefinition.ReadAssembly(@"E:\Projects\TestLibrary\bin\Debug\net6.0\TestLibrary.dll");
            var c = testLibrary.MainModule.ImportReference(compilerGeneratedConstructor);
            var module = testLibrary.MainModule;
            var class1 = module.GetType("TestLibrary.Class1");
            MethodReference objectConstructor = null;
            foreach (var n in class1.NestedTypes)
            {
                if (n.Name == "DB")
                {
                    foreach (var n2 in n.NestedTypes)
                    {
                        if (n2.Name == "<>c__DisplayClass4_0")
                        {
                            c = n2.CustomAttributes[0].Constructor;
                            foreach (var mm in n2.Methods)
                            {
                                if (mm.Name == ".ctor")
                                {
                                    objectConstructor = mm.Body.Instructions[1].Operand as MethodReference;
                                }
                            }
                            System.Console.WriteLine("A");
                        }
                    }
                    foreach (var m in n.Methods)
                    {
                        if (m.Name == "Test123")
                        {
                            var body = m.Body;
                            foreach (var inst in body.Instructions)
                            {
                                if (inst.OpCode == OpCodes.Ldftn)
                                {
                                    MonoHelper.ReplaceDisplayMethodWithDisplayClass(m, inst, new System.Collections.Generic.List<(TypeReference, string, Instruction)>() { (module.TypeSystem.Byte, "b", Instruction.Create(OpCodes.Ldarg_2)) }, c, objectConstructor);
                                    body.Optimize();
                                    break;
                                }
                            }
                            break;
                        }
                    }
                    break;
                }
            }
            testLibrary.Write(@"E:\Projects\TestLibrary\bin\Debug\net6.0\TestLibrary.modified.dll");
            return;*/
            var xmlStream = new StringReader(System.IO.File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "nlog.config")));
            var xmlReader = XmlReader.Create(xmlStream);
            LogManager.Configuration = new XmlLoggingConfiguration(xmlReader);
            NativeLibrary.SetDllImportResolver(Assembly.GetAssembly(typeof(Program)), (libraryName, assembly, searchPath) => {
                IntPtr handle;
                var path = Path.Combine(Configuration.DataDirectory, "libs", libraryName);
                NativeLibrary.TryLoad(path, assembly, searchPath, out handle);
                return handle;
            });
            Data.ModAPI.Initialize();
            Embedded.Extract();
            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
    }
}
