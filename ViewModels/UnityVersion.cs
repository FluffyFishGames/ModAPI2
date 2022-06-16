using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using Mono.Cecil;
using Newtonsoft.Json.Linq;
using ReactiveUI;

namespace ModAPI.ViewModels
{

    public class UnityVersion : ViewModelBase
    {
        /**
         * Fallback download URLs if regex matching doesn't work.
         */
        private static Dictionary<UnityVersion, string> WindowsDownloadLinks = new Dictionary<UnityVersion, string>()
        {
            {new UnityVersion("2021.1.21f1"), "https://download.unity3d.com/download_unity/f2d5d3c59f8c/Windows64EditorInstaller/UnitySetup64-2021.1.21f1.exe"},
            {new UnityVersion("2021.1.20f1"), "https://download.unity3d.com/download_unity/be552157821d/Windows64EditorInstaller/UnitySetup64-2021.1.20f1.exe"},
            {new UnityVersion("2021.1.19f1"), "https://download.unity3d.com/download_unity/5f5eb8bbdc25/Windows64EditorInstaller/UnitySetup64-2021.1.19f1.exe"},
            {new UnityVersion("2021.1.18f1"), "https://download.unity3d.com/download_unity/25bdc3efbc2d/Windows64EditorInstaller/UnitySetup64-2021.1.18f1.exe"},
            {new UnityVersion("2021.1.17f1"), "https://download.unity3d.com/download_unity/03b40fe07a36/Windows64EditorInstaller/UnitySetup64-2021.1.17f1.exe"},
            {new UnityVersion("2021.1.16f1"), "https://download.unity3d.com/download_unity/5fa502fca597/Windows64EditorInstaller/UnitySetup64-2021.1.16f1.exe"},
            {new UnityVersion("2021.1.15f1"), "https://download.unity3d.com/download_unity/e767a7370072/Windows64EditorInstaller/UnitySetup64-2021.1.15f1.exe"},
            {new UnityVersion("2021.1.14f1"), "https://download.unity3d.com/download_unity/51d2f824827f/Windows64EditorInstaller/UnitySetup64-2021.1.14f1.exe"},
            {new UnityVersion("2021.1.13f1"), "https://download.unity3d.com/download_unity/a03098edbbe0/Windows64EditorInstaller/UnitySetup64-2021.1.13f1.exe"},
            {new UnityVersion("2021.1.12f1"), "https://download.unity3d.com/download_unity/afcadd793de6/Windows64EditorInstaller/UnitySetup64-2021.1.12f1.exe"},
            {new UnityVersion("2021.1.11f1"), "https://download.unity3d.com/download_unity/4d8c25f7477e/Windows64EditorInstaller/UnitySetup64-2021.1.11f1.exe"},
            {new UnityVersion("2021.1.10f1"), "https://download.unity3d.com/download_unity/b15f561b2cef/Windows64EditorInstaller/UnitySetup64-2021.1.10f1.exe"},
            {new UnityVersion("2021.1.9f1"), "https://download.unity3d.com/download_unity/7a790e367ab3/Windows64EditorInstaller/UnitySetup64-2021.1.9f1.exe"},
            {new UnityVersion("2021.1.7f1"), "https://download.unity3d.com/download_unity/d91830b65d9b/Windows64EditorInstaller/UnitySetup64-2021.1.7f1.exe"},
            {new UnityVersion("2021.1.6f1"), "https://download.unity3d.com/download_unity/c0fade0cc7e9/Windows64EditorInstaller/UnitySetup64-2021.1.6f1.exe"},
            {new UnityVersion("2021.1.5f1"), "https://download.unity3d.com/download_unity/3737af19df53/Windows64EditorInstaller/UnitySetup64-2021.1.5f1.exe"},
            {new UnityVersion("2021.1.4f1"), "https://download.unity3d.com/download_unity/4cd64a618c1b/Windows64EditorInstaller/UnitySetup64-2021.1.4f1.exe"},
            {new UnityVersion("2021.1.3f1"), "https://download.unity3d.com/download_unity/4bef613afd59/Windows64EditorInstaller/UnitySetup64-2021.1.3f1.exe"},
            {new UnityVersion("2021.1.2f1"), "https://download.unity3d.com/download_unity/e5d502d80fbb/Windows64EditorInstaller/UnitySetup64-2021.1.2f1.exe"},
            {new UnityVersion("2021.1.1f1"), "https://download.unity3d.com/download_unity/6fdc41dfa55a/Windows64EditorInstaller/UnitySetup64-2021.1.1f1.exe"},
            {new UnityVersion("2021.1.0f1"), "https://download.unity3d.com/download_unity/61a549675243/Windows64EditorInstaller/UnitySetup64-2021.1.0f1.exe"},
            {new UnityVersion("2020.3.18f1"), "https://download.unity3d.com/download_unity/a7d1c678663c/Windows64EditorInstaller/UnitySetup64-2020.3.18f1.exe"},
            {new UnityVersion("2020.3.17f1"), "https://download.unity3d.com/download_unity/a4537701e4ab/Windows64EditorInstaller/UnitySetup64-2020.3.17f1.exe"},
            {new UnityVersion("2020.3.16f1"), "https://download.unity3d.com/download_unity/049d6eca3c44/Windows64EditorInstaller/UnitySetup64-2020.3.16f1.exe"},
            {new UnityVersion("2020.3.15f1"), "https://download.unity3d.com/download_unity/6cf78cb77498/Windows64EditorInstaller/UnitySetup64-2020.3.15f2.exe"},
            {new UnityVersion("2020.3.14f1"), "https://download.unity3d.com/download_unity/d0d1bb862f9d/Windows64EditorInstaller/UnitySetup64-2020.3.14f1.exe"},
            {new UnityVersion("2020.3.13f1"), "https://download.unity3d.com/download_unity/71691879b7f5/Windows64EditorInstaller/UnitySetup64-2020.3.13f1.exe"},
            {new UnityVersion("2020.3.12f1"), "https://download.unity3d.com/download_unity/b3b2c6512326/Windows64EditorInstaller/UnitySetup64-2020.3.12f1.exe"},
            {new UnityVersion("2020.3.11f1"), "https://download.unity3d.com/download_unity/99c7afb366b3/Windows64EditorInstaller/UnitySetup64-2020.3.11f1.exe"},
            {new UnityVersion("2020.3.10"), "https://download.unity3d.com/download_unity/297d780c91bc/Windows64EditorInstaller/UnitySetup64-2020.3.10f1.exe"},
            {new UnityVersion("2020.3.9"), "https://download.unity3d.com/download_unity/108be757e447/Windows64EditorInstaller/UnitySetup64-2020.3.9f1.exe"},
            {new UnityVersion("2020.3.8"), "https://download.unity3d.com/download_unity/507919d4fff5/Windows64EditorInstaller/UnitySetup64-2020.3.8f1.exe"},
            {new UnityVersion("2020.3.7"), "https://download.unity3d.com/download_unity/dd97f2c94397/Windows64EditorInstaller/UnitySetup64-2020.3.7f1.exe"},
            {new UnityVersion("2020.3.6"), "https://download.unity3d.com/download_unity/338bb68529b2/Windows64EditorInstaller/UnitySetup64-2020.3.6f1.exe"},
            {new UnityVersion("2020.3.5"), "https://download.unity3d.com/download_unity/8095aa901b9b/Windows64EditorInstaller/UnitySetup64-2020.3.5f1.exe"},
            {new UnityVersion("2020.3.4"), "https://download.unity3d.com/download_unity/0abb6314276a/Windows64EditorInstaller/UnitySetup64-2020.3.4f1.exe"},
            {new UnityVersion("2020.3.3"), "https://download.unity3d.com/download_unity/76626098c1c4/Windows64EditorInstaller/UnitySetup64-2020.3.3f1.exe"},
            {new UnityVersion("2020.3.2"), "https://download.unity3d.com/download_unity/8fd9074bf66c/Windows64EditorInstaller/UnitySetup64-2020.3.2f1.exe"},
            {new UnityVersion("2020.3.1"), "https://download.unity3d.com/download_unity/77a89f25062f/Windows64EditorInstaller/UnitySetup64-2020.3.1f1.exe"},
            {new UnityVersion("2020.3.0"), "https://download.unity3d.com/download_unity/c7b5465681fb/Windows64EditorInstaller/UnitySetup64-2020.3.0f1.exe"},
            {new UnityVersion("2020.2.7"), "https://download.unity3d.com/download_unity/c53830e277f1/Windows64EditorInstaller/UnitySetup64-2020.2.7f1.exe"},
            {new UnityVersion("2020.2.6"), "https://download.unity3d.com/download_unity/8a2143876886/Windows64EditorInstaller/UnitySetup64-2020.2.6f1.exe"},
            {new UnityVersion("2020.2.5"), "https://download.unity3d.com/download_unity/e2c53f129de5/Windows64EditorInstaller/UnitySetup64-2020.2.5f1.exe"},
            {new UnityVersion("2020.2.4"), "https://download.unity3d.com/download_unity/becced5a802b/Windows64EditorInstaller/UnitySetup64-2020.2.4f1.exe"},
            {new UnityVersion("2020.2.3"), "https://download.unity3d.com/download_unity/8ff31bc5bf5b/Windows64EditorInstaller/UnitySetup64-2020.2.3f1.exe"},
            {new UnityVersion("2020.2.2"), "https://download.unity3d.com/download_unity/068178b99f32/Windows64EditorInstaller/UnitySetup64-2020.2.2f1.exe"},
            {new UnityVersion("2020.2.1"), "https://download.unity3d.com/download_unity/270dd8c3da1c/Windows64EditorInstaller/UnitySetup64-2020.2.1f1.exe"},
            {new UnityVersion("2020.2.0"), "https://download.unity3d.com/download_unity/3721df5a8b28/Windows64EditorInstaller/UnitySetup64-2020.2.0f1.exe"},
            {new UnityVersion("2020.1.17"), "https://download.unity3d.com/download_unity/9957aee8edc2/Windows64EditorInstaller/UnitySetup64-2020.1.17f1.exe"},
            {new UnityVersion("2020.1.16"), "https://download.unity3d.com/download_unity/f483ad6465d6/Windows64EditorInstaller/UnitySetup64-2020.1.16f1.exe"},
            {new UnityVersion("2020.1.15"), "https://download.unity3d.com/download_unity/97d0ae02d19d/Windows64EditorInstaller/UnitySetup64-2020.1.15f1.exe"},
            {new UnityVersion("2020.1.14"), "https://download.unity3d.com/download_unity/d81f64f5201d/Windows64EditorInstaller/UnitySetup64-2020.1.14f1.exe"},
            {new UnityVersion("2020.1.13"), "https://download.unity3d.com/download_unity/5e24f28bfbc0/Windows64EditorInstaller/UnitySetup64-2020.1.13f1.exe"},
            {new UnityVersion("2020.1.12"), "https://download.unity3d.com/download_unity/55b56f0a86e3/Windows64EditorInstaller/UnitySetup64-2020.1.12f1.exe"},
            {new UnityVersion("2020.1.11"), "https://download.unity3d.com/download_unity/698c1113cef0/Windows64EditorInstaller/UnitySetup64-2020.1.11f1.exe"},
            {new UnityVersion("2020.1.10"), "https://download.unity3d.com/download_unity/974a9d56f159/Windows64EditorInstaller/UnitySetup64-2020.1.10f1.exe"},
            {new UnityVersion("2020.1.9"), "https://download.unity3d.com/download_unity/145f5172610f/Windows64EditorInstaller/UnitySetup64-2020.1.9f1.exe"},
            {new UnityVersion("2020.1.8"), "https://download.unity3d.com/download_unity/22e8c0b0c3ec/Windows64EditorInstaller/UnitySetup64-2020.1.8f1.exe"},
            {new UnityVersion("2020.1.7"), "https://download.unity3d.com/download_unity/064ffcdb64ad/Windows64EditorInstaller/UnitySetup64-2020.1.7f1.exe"},
            {new UnityVersion("2020.1.6"), "https://download.unity3d.com/download_unity/fc477ca6df10/Windows64EditorInstaller/UnitySetup64-2020.1.6f1.exe"},
            {new UnityVersion("2020.1.5"), "https://download.unity3d.com/download_unity/e025938fdedc/Windows64EditorInstaller/UnitySetup64-2020.1.5f1.exe"},
            {new UnityVersion("2020.1.4"), "https://download.unity3d.com/download_unity/fa717bb873ec/Windows64EditorInstaller/UnitySetup64-2020.1.4f1.exe"},
            {new UnityVersion("2020.1.3"), "https://download.unity3d.com/download_unity/cf5c4788e1d8/Windows64EditorInstaller/UnitySetup64-2020.1.3f1.exe"},
            {new UnityVersion("2020.1.2"), "https://download.unity3d.com/download_unity/7b32bc54ba47/Windows64EditorInstaller/UnitySetup64-2020.1.2f1.exe"},
            {new UnityVersion("2020.1.1"), "https://download.unity3d.com/download_unity/2285c3239188/Windows64EditorInstaller/UnitySetup64-2020.1.1f1.exe"},
            {new UnityVersion("2020.1.0"), "https://download.unity3d.com/download_unity/2ab9c4179772/Windows64EditorInstaller/UnitySetup64-2020.1.0f1.exe"},
            {new UnityVersion("2019.4.30"), "https://download.unity3d.com/download_unity/e8c891080a1f/Windows64EditorInstaller/UnitySetup64-2019.4.30f1.exe"},
            {new UnityVersion("2019.4.29"), "https://download.unity3d.com/download_unity/0eeae20b1d82/Windows64EditorInstaller/UnitySetup64-2019.4.29f1.exe"},
            {new UnityVersion("2019.4.28"), "https://download.unity3d.com/download_unity/1381962e9d08/Windows64EditorInstaller/UnitySetup64-2019.4.28f1.exe"},
            {new UnityVersion("2019.4.27"), "https://download.unity3d.com/download_unity/23dc10685eb4/Windows64EditorInstaller/UnitySetup64-2019.4.27f1.exe"},
            {new UnityVersion("2019.4.26"), "https://download.unity3d.com/download_unity/e0392c6b2363/Windows64EditorInstaller/UnitySetup64-2019.4.26f1.exe"},
            {new UnityVersion("2019.4.25"), "https://download.unity3d.com/download_unity/01a0494af254/Windows64EditorInstaller/UnitySetup64-2019.4.25f1.exe"},
            {new UnityVersion("2019.4.24"), "https://download.unity3d.com/download_unity/5da6f0345e82/Windows64EditorInstaller/UnitySetup64-2019.4.24f1.exe"},
            {new UnityVersion("2019.4.23"), "https://download.unity3d.com/download_unity/3f4e01f1a5ec/Windows64EditorInstaller/UnitySetup64-2019.4.23f1.exe"},
            {new UnityVersion("2019.4.22"), "https://download.unity3d.com/download_unity/9fdda2fe27ad/Windows64EditorInstaller/UnitySetup64-2019.4.22f1.exe"},
            {new UnityVersion("2019.4.21"), "https://download.unity3d.com/download_unity/b76dac84db26/Windows64EditorInstaller/UnitySetup64-2019.4.21f1.exe"},
            {new UnityVersion("2019.4.20"), "https://download.unity3d.com/download_unity/6dd1c08eedfa/Windows64EditorInstaller/UnitySetup64-2019.4.20f1.exe"},
            {new UnityVersion("2019.4.19"), "https://download.unity3d.com/download_unity/ca5b14067cec/Windows64EditorInstaller/UnitySetup64-2019.4.19f1.exe"},
            {new UnityVersion("2019.4.18"), "https://download.unity3d.com/download_unity/3310a4d4f880/Windows64EditorInstaller/UnitySetup64-2019.4.18f1.exe"},
            {new UnityVersion("2019.4.17"), "https://download.unity3d.com/download_unity/667c8606c536/Windows64EditorInstaller/UnitySetup64-2019.4.17f1.exe"},
            {new UnityVersion("2019.4.16"), "https://download.unity3d.com/download_unity/e05b6e02d63e/Windows64EditorInstaller/UnitySetup64-2019.4.16f1.exe"},
            {new UnityVersion("2019.4.15"), "https://download.unity3d.com/download_unity/fbf367ac14e9/Windows64EditorInstaller/UnitySetup64-2019.4.15f1.exe"},
            {new UnityVersion("2019.4.14"), "https://download.unity3d.com/download_unity/4037e52648cd/Windows64EditorInstaller/UnitySetup64-2019.4.14f1.exe"},
            {new UnityVersion("2019.4.13"), "https://download.unity3d.com/download_unity/518737b1de84/Windows64EditorInstaller/UnitySetup64-2019.4.13f1.exe"},
            {new UnityVersion("2019.4.12"), "https://download.unity3d.com/download_unity/225e826a680e/Windows64EditorInstaller/UnitySetup64-2019.4.12f1.exe"},
            {new UnityVersion("2019.4.11"), "https://download.unity3d.com/download_unity/2d9804dddde7/Windows64EditorInstaller/UnitySetup64-2019.4.11f1.exe"},
            {new UnityVersion("2019.4.10"), "https://download.unity3d.com/download_unity/5311b3af6f69/Windows64EditorInstaller/UnitySetup64-2019.4.10f1.exe"},
            {new UnityVersion("2019.4.9"), "https://download.unity3d.com/download_unity/50fe8a171dd9/Windows64EditorInstaller/UnitySetup64-2019.4.9f1.exe"},
            {new UnityVersion("2019.4.8"), "https://download.unity3d.com/download_unity/60781d942082/Windows64EditorInstaller/UnitySetup64-2019.4.8f1.exe"},
            {new UnityVersion("2019.4.7"), "https://download.unity3d.com/download_unity/e992b1a16e65/Windows64EditorInstaller/UnitySetup64-2019.4.7f1.exe"},
            {new UnityVersion("2019.4.6"), "https://download.unity3d.com/download_unity/a7aea80e3716/Windows64EditorInstaller/UnitySetup64-2019.4.6f1.exe"},
            {new UnityVersion("2019.4.5"), "https://download.unity3d.com/download_unity/81610f64359c/Windows64EditorInstaller/UnitySetup64-2019.4.5f1.exe"},
            {new UnityVersion("2019.4.4"), "https://download.unity3d.com/download_unity/1f1dac67805b/Windows64EditorInstaller/UnitySetup64-2019.4.4f1.exe"},
            {new UnityVersion("2019.4.3"), "https://download.unity3d.com/download_unity/f880dceab6fe/Windows64EditorInstaller/UnitySetup64-2019.4.3f1.exe"},
            {new UnityVersion("2019.4.2"), "https://download.unity3d.com/download_unity/20b4642a3455/Windows64EditorInstaller/UnitySetup64-2019.4.2f1.exe"},
            {new UnityVersion("2019.4.1"), "https://download.unity3d.com/download_unity/e6c045e14e4e/Windows64EditorInstaller/UnitySetup64-2019.4.1f1.exe"},
            {new UnityVersion("2019.4.0"), "https://download.unity3d.com/download_unity/0af376155913/Windows64EditorInstaller/UnitySetup64-2019.4.0f1.exe"},
            {new UnityVersion("2019.3.15"), "https://download.unity3d.com/download_unity/59ff3e03856d/Windows64EditorInstaller/UnitySetup64-2019.3.15f1.exe"},
            {new UnityVersion("2019.3.14"), "https://download.unity3d.com/download_unity/2b330bf6d2d8/Windows64EditorInstaller/UnitySetup64-2019.3.14f1.exe"},
            {new UnityVersion("2019.3.13"), "https://download.unity3d.com/download_unity/d4ddf0d95db9/Windows64EditorInstaller/UnitySetup64-2019.3.13f1.exe"},
            {new UnityVersion("2019.3.12"), "https://download.unity3d.com/download_unity/84b23722532d/Windows64EditorInstaller/UnitySetup64-2019.3.12f1.exe"},
            {new UnityVersion("2019.3.11"), "https://download.unity3d.com/download_unity/ceef2d848e70/Windows64EditorInstaller/UnitySetup64-2019.3.11f1.exe"},
            {new UnityVersion("2019.3.10"), "https://download.unity3d.com/download_unity/5968d7f82152/Windows64EditorInstaller/UnitySetup64-2019.3.10f1.exe"},
            {new UnityVersion("2019.3.9"), "https://download.unity3d.com/download_unity/e6e740a1c473/Windows64EditorInstaller/UnitySetup64-2019.3.9f1.exe"},
            {new UnityVersion("2019.3.8"), "https://download.unity3d.com/download_unity/4ba98e9386ed/Windows64EditorInstaller/UnitySetup64-2019.3.8f1.exe"},
            {new UnityVersion("2019.3.7"), "https://download.unity3d.com/download_unity/6437fd74d35d/Windows64EditorInstaller/UnitySetup64-2019.3.7f1.exe"},
            {new UnityVersion("2019.3.6"), "https://download.unity3d.com/download_unity/5c3fb0a11183/Windows64EditorInstaller/UnitySetup64-2019.3.6f1.exe"},
            {new UnityVersion("2019.3.5"), "https://download.unity3d.com/download_unity/d691e07d38ef/Windows64EditorInstaller/UnitySetup64-2019.3.5f1.exe"},
            {new UnityVersion("2019.3.4"), "https://download.unity3d.com/download_unity/4f139db2fdbd/Windows64EditorInstaller/UnitySetup64-2019.3.4f1.exe"},
            {new UnityVersion("2019.3.3"), "https://download.unity3d.com/download_unity/7ceaae5f7503/Windows64EditorInstaller/UnitySetup64-2019.3.3f1.exe"},
            {new UnityVersion("2019.3.2"), "https://download.unity3d.com/download_unity/c46a3a38511e/Windows64EditorInstaller/UnitySetup64-2019.3.2f1.exe"},
            {new UnityVersion("2019.3.1"), "https://download.unity3d.com/download_unity/89d6087839c2/Windows64EditorInstaller/UnitySetup64-2019.3.1f1.exe"},
            {new UnityVersion("2019.3.0"), "https://download.unity3d.com/download_unity/27ab2135bccf/Windows64EditorInstaller/UnitySetup64-2019.3.0f6.exe"},
            {new UnityVersion("2019.2.21"), "https://download.unity3d.com/download_unity/9d528d026557/Windows64EditorInstaller/UnitySetup64-2019.2.21f1.exe"},
            {new UnityVersion("2019.2.20"), "https://download.unity3d.com/download_unity/c67d00285037/Windows64EditorInstaller/UnitySetup64-2019.2.20f1.exe"},
            {new UnityVersion("2019.2.19"), "https://download.unity3d.com/download_unity/929ab4d01772/Windows64EditorInstaller/UnitySetup64-2019.2.19f1.exe"},
            {new UnityVersion("2019.2.18"), "https://download.unity3d.com/download_unity/bbf64de26e34/Windows64EditorInstaller/UnitySetup64-2019.2.18f1.exe"},
            {new UnityVersion("2019.2.17"), "https://download.unity3d.com/download_unity/8e603399ca02/Windows64EditorInstaller/UnitySetup64-2019.2.17f1.exe"},
            {new UnityVersion("2019.2.16"), "https://download.unity3d.com/download_unity/b9898e2d04a4/Windows64EditorInstaller/UnitySetup64-2019.2.16f1.exe"},
            {new UnityVersion("2019.2.15"), "https://download.unity3d.com/download_unity/dcb72c2e9334/Windows64EditorInstaller/UnitySetup64-2019.2.15f1.exe"},
            {new UnityVersion("2019.2.14"), "https://download.unity3d.com/download_unity/49dd4e9fa428/Windows64EditorInstaller/UnitySetup64-2019.2.14f1.exe"},
            {new UnityVersion("2019.2.13"), "https://download.unity3d.com/download_unity/e20f6c7e5017/Windows64EditorInstaller/UnitySetup64-2019.2.13f1.exe"},
            {new UnityVersion("2019.2.12"), "https://download.unity3d.com/download_unity/b1a7e1fb4fa5/Windows64EditorInstaller/UnitySetup64-2019.2.12f1.exe"},
            {new UnityVersion("2019.2.11"), "https://download.unity3d.com/download_unity/5f859a4cfee5/Windows64EditorInstaller/UnitySetup64-2019.2.11f1.exe"},
            {new UnityVersion("2019.2.10"), "https://download.unity3d.com/download_unity/923acd2d43aa/Windows64EditorInstaller/UnitySetup64-2019.2.10f1.exe"},
            {new UnityVersion("2019.2.9"), "https://download.unity3d.com/download_unity/ebce4d76e6e8/Windows64EditorInstaller/UnitySetup64-2019.2.9f1.exe"},
            {new UnityVersion("2019.2.8"), "https://download.unity3d.com/download_unity/ff5b465c8d13/Windows64EditorInstaller/UnitySetup64-2019.2.8f1.exe"},
            {new UnityVersion("2019.2.7"), "https://download.unity3d.com/download_unity/c96f78eb5904/Windows64EditorInstaller/UnitySetup64-2019.2.7f2.exe"},
            {new UnityVersion("2019.2.6"), "https://download.unity3d.com/download_unity/fe82a0e88406/Windows64EditorInstaller/UnitySetup64-2019.2.6f1.exe"},
            {new UnityVersion("2019.2.5"), "https://download.unity3d.com/download_unity/9dace1eed4cc/Windows64EditorInstaller/UnitySetup64-2019.2.5f1.exe"},
            {new UnityVersion("2019.2.4"), "https://download.unity3d.com/download_unity/c63b2af89a85/Windows64EditorInstaller/UnitySetup64-2019.2.4f1.exe"},
            {new UnityVersion("2019.2.3"), "https://download.unity3d.com/download_unity/8e55c27a4621/Windows64EditorInstaller/UnitySetup64-2019.2.3f1.exe"},
            {new UnityVersion("2019.2.2"), "https://download.unity3d.com/download_unity/ab112815d860/Windows64EditorInstaller/UnitySetup64-2019.2.2f1.exe"},
            {new UnityVersion("2019.2.1"), "https://download.unity3d.com/download_unity/ca4d5af0be6f/Windows64EditorInstaller/UnitySetup64-2019.2.1f1.exe"},
            {new UnityVersion("2019.2.0"), "https://download.unity3d.com/download_unity/20c1667945cf/Windows64EditorInstaller/UnitySetup64-2019.2.0f1.exe"},
            {new UnityVersion("2019.1.14"), "https://download.unity3d.com/download_unity/148b5891095a/Windows64EditorInstaller/UnitySetup64-2019.1.14f1.exe"},
            {new UnityVersion("2019.1.13"), "https://download.unity3d.com/download_unity/b5956c0a61e7/Windows64EditorInstaller/UnitySetup64-2019.1.13f1.exe"},
            {new UnityVersion("2019.1.12"), "https://download.unity3d.com/download_unity/f04f5427219e/Windows64EditorInstaller/UnitySetup64-2019.1.12f1.exe"},
            {new UnityVersion("2019.1.11"), "https://download.unity3d.com/download_unity/9b001d489a54/Windows64EditorInstaller/UnitySetup64-2019.1.11f1.exe"},
            {new UnityVersion("2019.1.10"), "https://download.unity3d.com/download_unity/f007ed779b7a/Windows64EditorInstaller/UnitySetup64-2019.1.10f1.exe"},
            {new UnityVersion("2019.1.9"), "https://download.unity3d.com/download_unity/d5f1b37da199/Windows64EditorInstaller/UnitySetup64-2019.1.9f1.exe"},
            {new UnityVersion("2019.1.8"), "https://download.unity3d.com/download_unity/7938dd008a75/Windows64EditorInstaller/UnitySetup64-2019.1.8f1.exe"},
            {new UnityVersion("2019.1.7"), "https://download.unity3d.com/download_unity/f3c4928e5742/Windows64EditorInstaller/UnitySetup64-2019.1.7f1.exe"},
            {new UnityVersion("2019.1.6"), "https://download.unity3d.com/download_unity/f2970305fe1c/Windows64EditorInstaller/UnitySetup64-2019.1.6f1.exe"},
            {new UnityVersion("2019.1.5"), "https://download.unity3d.com/download_unity/0ca0f5646614/Windows64EditorInstaller/UnitySetup64-2019.1.5f1.exe"},
            {new UnityVersion("2019.1.4"), "https://download.unity3d.com/download_unity/ffa3a7a2dd7d/Windows64EditorInstaller/UnitySetup64-2019.1.4f1.exe"},
            {new UnityVersion("2019.1.3"), "https://download.unity3d.com/download_unity/dc414eb9ed43/Windows64EditorInstaller/UnitySetup64-2019.1.3f1.exe"},
            {new UnityVersion("2019.1.2"), "https://download.unity3d.com/download_unity/3e18427e571f/Windows64EditorInstaller/UnitySetup64-2019.1.2f1.exe"},
            {new UnityVersion("2019.1.1"), "https://download.unity3d.com/download_unity/fef62e97e63b/Windows64EditorInstaller/UnitySetup64-2019.1.1f1.exe"},
            {new UnityVersion("2019.1.0"), "https://download.unity3d.com/download_unity/292b93d75a2c/Windows64EditorInstaller/UnitySetup64-2019.1.0f2.exe"},
            {new UnityVersion("2018.4.36"), "https://download.unity3d.com/download_unity/6cd387d23174/Windows64EditorInstaller/UnitySetup64-2018.4.36f1.exe"},
            {new UnityVersion("2018.4.35"), "https://download.unity3d.com/download_unity/dbb5675dce2d/Windows64EditorInstaller/UnitySetup64-2018.4.35f1.exe"},
            {new UnityVersion("2018.4.34"), "https://download.unity3d.com/download_unity/ae2afac172fb/Windows64EditorInstaller/UnitySetup64-2018.4.34f1.exe"},
            {new UnityVersion("2018.4.33"), "https://download.unity3d.com/download_unity/d75f7e9df24c/Windows64EditorInstaller/UnitySetup64-2018.4.33f1.exe"},
            {new UnityVersion("2018.4.32"), "https://download.unity3d.com/download_unity/fba45da84107/Windows64EditorInstaller/UnitySetup64-2018.4.32f1.exe"},
            {new UnityVersion("2018.4.31"), "https://download.unity3d.com/download_unity/212ea663d844/Windows64EditorInstaller/UnitySetup64-2018.4.31f1.exe"},
            {new UnityVersion("2018.4.30"), "https://download.unity3d.com/download_unity/c698a062d8e6/Windows64EditorInstaller/UnitySetup64-2018.4.30f1.exe"},
            {new UnityVersion("2018.4.29"), "https://download.unity3d.com/download_unity/50cce2edf27f/Windows64EditorInstaller/UnitySetup64-2018.4.29f1.exe"},
            {new UnityVersion("2018.4.28"), "https://download.unity3d.com/download_unity/a2d4f71491a4/Windows64EditorInstaller/UnitySetup64-2018.4.28f1.exe"},
            {new UnityVersion("2018.4.27"), "https://download.unity3d.com/download_unity/4e283b7d3f88/Windows64EditorInstaller/UnitySetup64-2018.4.27f1.exe"},
            {new UnityVersion("2018.4.26"), "https://download.unity3d.com/download_unity/a7ac1c6396db/Windows64EditorInstaller/UnitySetup64-2018.4.26f1.exe"},
            {new UnityVersion("2018.4.25"), "https://download.unity3d.com/download_unity/b07bfa0a8827/Windows64EditorInstaller/UnitySetup64-2018.4.25f1.exe"},
            {new UnityVersion("2018.4.24"), "https://download.unity3d.com/download_unity/3071911a89e9/Windows64EditorInstaller/UnitySetup64-2018.4.24f1.exe"},
            {new UnityVersion("2018.4.23"), "https://download.unity3d.com/download_unity/c9cf1a90e812/Windows64EditorInstaller/UnitySetup64-2018.4.23f1.exe"},
            {new UnityVersion("2018.4.22"), "https://download.unity3d.com/download_unity/3362ffbb7aa1/Windows64EditorInstaller/UnitySetup64-2018.4.22f1.exe"},
            {new UnityVersion("2018.4.21"), "https://download.unity3d.com/download_unity/fd3915227633/Windows64EditorInstaller/UnitySetup64-2018.4.21f1.exe"},
            {new UnityVersion("2018.4.20"), "https://download.unity3d.com/download_unity/008688490035/Windows64EditorInstaller/UnitySetup64-2018.4.20f1.exe"},
            {new UnityVersion("2018.4.19"), "https://download.unity3d.com/download_unity/459f70f82ea4/Windows64EditorInstaller/UnitySetup64-2018.4.19f1.exe"},
            {new UnityVersion("2018.4.18"), "https://download.unity3d.com/download_unity/61fce66342ad/Windows64EditorInstaller/UnitySetup64-2018.4.18f1.exe"},
            {new UnityVersion("2018.4.17"), "https://download.unity3d.com/download_unity/b830f56f42f0/Windows64EditorInstaller/UnitySetup64-2018.4.17f1.exe"},
            {new UnityVersion("2018.4.16"), "https://download.unity3d.com/download_unity/e6e9ca02b32a/Windows64EditorInstaller/UnitySetup64-2018.4.16f1.exe"},
            {new UnityVersion("2018.4.15"), "https://download.unity3d.com/download_unity/13f5a1bf9ca1/Windows64EditorInstaller/UnitySetup64-2018.4.15f1.exe"},
            {new UnityVersion("2018.4.14"), "https://download.unity3d.com/download_unity/05119b33d0b7/Windows64EditorInstaller/UnitySetup64-2018.4.14f1.exe"},
            {new UnityVersion("2018.4.13"), "https://download.unity3d.com/download_unity/497f083a43af/Windows64EditorInstaller/UnitySetup64-2018.4.13f1.exe"},
            {new UnityVersion("2018.4.12"), "https://download.unity3d.com/download_unity/59ddc4c59b4f/Windows64EditorInstaller/UnitySetup64-2018.4.12f1.exe"},
            {new UnityVersion("2018.4.11"), "https://download.unity3d.com/download_unity/7098af2f11ea/Windows64EditorInstaller/UnitySetup64-2018.4.11f1.exe"},
            {new UnityVersion("2018.4.10"), "https://download.unity3d.com/download_unity/a0470569e97b/Windows64EditorInstaller/UnitySetup64-2018.4.10f1.exe"},
            {new UnityVersion("2018.4.9"), "https://download.unity3d.com/download_unity/ca372476eaba/Windows64EditorInstaller/UnitySetup64-2018.4.9f1.exe"},
            {new UnityVersion("2018.4.8"), "https://download.unity3d.com/download_unity/9bc9d983d803/Windows64EditorInstaller/UnitySetup64-2018.4.8f1.exe"},
            {new UnityVersion("2018.4.7"), "https://download.unity3d.com/download_unity/b9a993fd1334/Windows64EditorInstaller/UnitySetup64-2018.4.7f1.exe"},
            {new UnityVersion("2018.4.6"), "https://download.unity3d.com/download_unity/cde1bbcc9f0d/Windows64EditorInstaller/UnitySetup64-2018.4.6f1.exe"},
            {new UnityVersion("2018.4.5"), "https://download.unity3d.com/download_unity/7b38f8ac282e/Windows64EditorInstaller/UnitySetup64-2018.4.5f1.exe"},
            {new UnityVersion("2018.4.4"), "https://download.unity3d.com/download_unity/5440768ff61c/Windows64EditorInstaller/UnitySetup64-2018.4.4f1.exe"},
            {new UnityVersion("2018.4.3"), "https://download.unity3d.com/download_unity/8a9509a5aff9/Windows64EditorInstaller/UnitySetup64-2018.4.3f1.exe"},
            {new UnityVersion("2018.4.2"), "https://download.unity3d.com/download_unity/d6fb3630ea75/Windows64EditorInstaller/UnitySetup64-2018.4.2f1.exe"},
            {new UnityVersion("2018.4.1"), "https://download.unity3d.com/download_unity/b7c424a951c0/Windows64EditorInstaller/UnitySetup64-2018.4.1f1.exe"},
            {new UnityVersion("2018.4.0"), "https://download.unity3d.com/download_unity/b6ffa8986c8d/Windows64EditorInstaller/UnitySetup64-2018.4.0f1.exe"},
            {new UnityVersion("2018.3.14"), "https://download.unity3d.com/download_unity/d0e9f15437b1/Windows64EditorInstaller/UnitySetup64-2018.3.14f1.exe"},
            {new UnityVersion("2018.3.13"), "https://download.unity3d.com/download_unity/06548a9e9582/Windows64EditorInstaller/UnitySetup64-2018.3.13f1.exe"},
            {new UnityVersion("2018.3.12"), "https://download.unity3d.com/download_unity/8afd630d1f5b/Windows64EditorInstaller/UnitySetup64-2018.3.12f1.exe"},
            {new UnityVersion("2018.3.11"), "https://download.unity3d.com/download_unity/5063218e4ab8/Windows64EditorInstaller/UnitySetup64-2018.3.11f1.exe"},
            {new UnityVersion("2018.3.10"), "https://download.unity3d.com/download_unity/f88de2c96e63/Windows64EditorInstaller/UnitySetup64-2018.3.10f1.exe"},
            {new UnityVersion("2018.3.9"), "https://download.unity3d.com/download_unity/947e1ea5aa8d/Windows64EditorInstaller/UnitySetup64-2018.3.9f1.exe"},
            {new UnityVersion("2018.3.8"), "https://download.unity3d.com/download_unity/fc0fe30d6d91/Windows64EditorInstaller/UnitySetup64-2018.3.8f1.exe"},
            {new UnityVersion("2018.3.7"), "https://download.unity3d.com/download_unity/9e14d22a41bb/Windows64EditorInstaller/UnitySetup64-2018.3.7f1.exe"},
            {new UnityVersion("2018.3.6"), "https://download.unity3d.com/download_unity/a220877bc173/Windows64EditorInstaller/UnitySetup64-2018.3.6f1.exe"},
            {new UnityVersion("2018.3.5"), "https://download.unity3d.com/download_unity/76b3e37670a4/Windows64EditorInstaller/UnitySetup64-2018.3.5f1.exe"},
            {new UnityVersion("2018.3.4"), "https://download.unity3d.com/download_unity/1d952368ca3a/Windows64EditorInstaller/UnitySetup64-2018.3.4f1.exe"},
            {new UnityVersion("2018.3.3"), "https://download.unity3d.com/download_unity/393bae82dbb8/Windows64EditorInstaller/UnitySetup64-2018.3.3f1.exe"},
            {new UnityVersion("2018.3.2"), "https://download.unity3d.com/download_unity/b3c100a4b73a/Windows64EditorInstaller/UnitySetup64-2018.3.2f1.exe"},
            {new UnityVersion("2018.3.1"), "https://download.unity3d.com/download_unity/bb579dc42f1d/Windows64EditorInstaller/UnitySetup64-2018.3.1f1.exe"},
            {new UnityVersion("2018.3.0"), "https://download.unity3d.com/download_unity/6e9a27477296/Windows64EditorInstaller/UnitySetup64-2018.3.0f2.exe"},
            {new UnityVersion("2018.2.21"), "https://download.unity3d.com/download_unity/a122f5dc316d/Windows64EditorInstaller/UnitySetup64-2018.2.21f1.exe"},
            {new UnityVersion("2018.2.20"), "https://download.unity3d.com/download_unity/cef3e6c0c622/Windows64EditorInstaller/UnitySetup64-2018.2.20f1.exe"},
            {new UnityVersion("2018.2.19"), "https://download.unity3d.com/download_unity/06990f28ba00/Windows64EditorInstaller/UnitySetup64-2018.2.19f1.exe"},
            {new UnityVersion("2018.2.18"), "https://download.unity3d.com/download_unity/4550892b6062/Windows64EditorInstaller/UnitySetup64-2018.2.18f1.exe"},
            {new UnityVersion("2018.2.17"), "https://download.unity3d.com/download_unity/88933597c842/Windows64EditorInstaller/UnitySetup64-2018.2.17f1.exe"},
            {new UnityVersion("2018.2.16"), "https://download.unity3d.com/download_unity/39a4ac3d51f6/Windows64EditorInstaller/UnitySetup64-2018.2.16f1.exe"},
            {new UnityVersion("2018.2.15"), "https://download.unity3d.com/download_unity/65e0713a5949/Windows64EditorInstaller/UnitySetup64-2018.2.15f1.exe"},
            {new UnityVersion("2018.2.14"), "https://download.unity3d.com/download_unity/3262fb3b0716/Windows64EditorInstaller/UnitySetup64-2018.2.14f1.exe"},
            {new UnityVersion("2018.2.13"), "https://download.unity3d.com/download_unity/83fbdcd35118/Windows64EditorInstaller/UnitySetup64-2018.2.13f1.exe"},
            {new UnityVersion("2018.2.12"), "https://download.unity3d.com/download_unity/0a46ddfcfad4/Windows64EditorInstaller/UnitySetup64-2018.2.12f1.exe"},
            {new UnityVersion("2018.2.11"), "https://download.unity3d.com/download_unity/38bd7dec5000/Windows64EditorInstaller/UnitySetup64-2018.2.11f1.exe"},
            {new UnityVersion("2018.2.10"), "https://download.unity3d.com/download_unity/674aa5a67ed5/Windows64EditorInstaller/UnitySetup64-2018.2.10f1.exe"},
            {new UnityVersion("2018.2.9"), "https://download.unity3d.com/download_unity/2207421190e9/Windows64EditorInstaller/UnitySetup64-2018.2.9f1.exe"},
            {new UnityVersion("2018.2.8"), "https://download.unity3d.com/download_unity/ae1180820377/Windows64EditorInstaller/UnitySetup64-2018.2.8f1.exe"},
            {new UnityVersion("2018.2.7"), "https://download.unity3d.com/download_unity/4ebd28dd9664/Windows64EditorInstaller/UnitySetup64-2018.2.7f1.exe"},
            {new UnityVersion("2018.2.6"), "https://download.unity3d.com/download_unity/c591d9a97a0b/Windows64EditorInstaller/UnitySetup64-2018.2.6f1.exe"},
            {new UnityVersion("2018.2.5"), "https://download.unity3d.com/download_unity/3071d1717b71/Windows64EditorInstaller/UnitySetup64-2018.2.5f1.exe"},
            {new UnityVersion("2018.2.4"), "https://download.unity3d.com/download_unity/cb262d9ddeaf/Windows64EditorInstaller/UnitySetup64-2018.2.4f1.exe"},
            {new UnityVersion("2018.2.3"), "https://download.unity3d.com/download_unity/1431a7d2ced7/Windows64EditorInstaller/UnitySetup64-2018.2.3f1.exe"},
            {new UnityVersion("2018.2.2"), "https://download.unity3d.com/download_unity/c18cef34cbcd/Windows64EditorInstaller/UnitySetup64-2018.2.2f1.exe"},
            {new UnityVersion("2018.2.1"), "https://download.unity3d.com/download_unity/1a9968d9f99c/Windows64EditorInstaller/UnitySetup64-2018.2.1f1.exe"},
            {new UnityVersion("2018.2.0"), "https://download.unity3d.com/download_unity/787658998520/Windows64EditorInstaller/UnitySetup64-2018.2.0f2.exe"},
            {new UnityVersion("2018.1.9"), "https://download.unity3d.com/download_unity/a6cc294b73ee/Windows64EditorInstaller/UnitySetup64-2018.1.9f2.exe"},
            {new UnityVersion("2018.1.8"), "https://download.unity3d.com/download_unity/26051d4de9e9/Windows64EditorInstaller/UnitySetup64-2018.1.8f1.exe"},
            {new UnityVersion("2018.1.7"), "https://download.unity3d.com/download_unity/4cb482063d12/Windows64EditorInstaller/UnitySetup64-2018.1.7f1.exe"},
            {new UnityVersion("2018.1.6"), "https://download.unity3d.com/download_unity/57cc34175ccf/Windows64EditorInstaller/UnitySetup64-2018.1.6f1.exe"},
            {new UnityVersion("2018.1.5"), "https://download.unity3d.com/download_unity/732dbf75922d/Windows64EditorInstaller/UnitySetup64-2018.1.5f1.exe"},
            {new UnityVersion("2018.1.4"), "https://download.unity3d.com/download_unity/1a308f4ebef1/Windows64EditorInstaller/UnitySetup64-2018.1.4f1.exe"},
            {new UnityVersion("2018.1.3"), "https://download.unity3d.com/download_unity/a53ad04f7c7f/Windows64EditorInstaller/UnitySetup64-2018.1.3f1.exe"},
            {new UnityVersion("2018.1.2"), "https://download.unity3d.com/download_unity/a46d718d282d/Windows64EditorInstaller/UnitySetup64-2018.1.2f1.exe"},
            {new UnityVersion("2018.1.1"), "https://download.unity3d.com/download_unity/b8cbb5de9840/Windows64EditorInstaller/UnitySetup64-2018.1.1f1.exe"},
            {new UnityVersion("2018.1.0"), "https://download.unity3d.com/download_unity/d4d99f31acba/Windows64EditorInstaller/UnitySetup64-2018.1.0f2.exe"},
            {new UnityVersion("2017.4.40"), "https://download.unity3d.com/download_unity/6e14067f8a9a/Windows64EditorInstaller/UnitySetup64-2017.4.40f1.exe"},
            {new UnityVersion("2017.4.39"), "https://download.unity3d.com/download_unity/947131c5be7e/Windows64EditorInstaller/UnitySetup64-2017.4.39f1.exe"},
            {new UnityVersion("2017.4.38"), "https://download.unity3d.com/download_unity/82ac2fb100ce/Windows64EditorInstaller/UnitySetup64-2017.4.38f1.exe"},
            {new UnityVersion("2017.4.37"), "https://download.unity3d.com/download_unity/78b69503ebc4/Windows64EditorInstaller/UnitySetup64-2017.4.37f1.exe"},
            {new UnityVersion("2017.4.36"), "https://download.unity3d.com/download_unity/c663def8414c/Windows64EditorInstaller/UnitySetup64-2017.4.36f1.exe"},
            {new UnityVersion("2017.4.35"), "https://download.unity3d.com/download_unity/e57a7bcbbf0b/Windows64EditorInstaller/UnitySetup64-2017.4.35f1.exe"},
            {new UnityVersion("2017.4.34"), "https://download.unity3d.com/download_unity/121f18246307/Windows64EditorInstaller/UnitySetup64-2017.4.34f1.exe"},
            {new UnityVersion("2017.4.33"), "https://download.unity3d.com/download_unity/a8557a619e24/Windows64EditorInstaller/UnitySetup64-2017.4.33f1.exe"},
            {new UnityVersion("2017.4.32"), "https://download.unity3d.com/download_unity/4da3ed968770/Windows64EditorInstaller/UnitySetup64-2017.4.32f1.exe"},
            {new UnityVersion("2017.4.31"), "https://download.unity3d.com/download_unity/9c8dbc3421cb/Windows64EditorInstaller/UnitySetup64-2017.4.31f1.exe"},
            {new UnityVersion("2017.4.30"), "https://download.unity3d.com/download_unity/c6fa43736cae/Windows64EditorInstaller/UnitySetup64-2017.4.30f1.exe"},
            {new UnityVersion("2017.4.29"), "https://download.unity3d.com/download_unity/06508aa14ca1/Windows64EditorInstaller/UnitySetup64-2017.4.29f1.exe"},
            {new UnityVersion("2017.4.28"), "https://download.unity3d.com/download_unity/e3a0f7dd2097/Windows64EditorInstaller/UnitySetup64-2017.4.28f1.exe"},
            {new UnityVersion("2017.4.27"), "https://download.unity3d.com/download_unity/0c4b856e4c6e/Windows64EditorInstaller/UnitySetup64-2017.4.27f1.exe"},
            {new UnityVersion("2017.4.26"), "https://download.unity3d.com/download_unity/3b349d10f010/Windows64EditorInstaller/UnitySetup64-2017.4.26f1.exe"},
            {new UnityVersion("2017.4.25"), "https://download.unity3d.com/download_unity/9cba1c3a94f1/Windows64EditorInstaller/UnitySetup64-2017.4.25f1.exe"},
            {new UnityVersion("2017.4.24"), "https://download.unity3d.com/download_unity/786769fc3439/Windows64EditorInstaller/UnitySetup64-2017.4.24f1.exe"},
            {new UnityVersion("2017.4.23"), "https://download.unity3d.com/download_unity/f80c8a98b1b5/Windows64EditorInstaller/UnitySetup64-2017.4.23f1.exe"},
            {new UnityVersion("2017.4.22"), "https://download.unity3d.com/download_unity/eb4bc6fa7f1d/Windows64EditorInstaller/UnitySetup64-2017.4.22f1.exe"},
            {new UnityVersion("2017.4.21"), "https://download.unity3d.com/download_unity/de35fe252486/Windows64EditorInstaller/UnitySetup64-2017.4.21f1.exe"},
            {new UnityVersion("2017.4.20"), "https://download.unity3d.com/download_unity/413dbd19b6dc/Windows64EditorInstaller/UnitySetup64-2017.4.20f2.exe"},
            {new UnityVersion("2017.4.19"), "https://download.unity3d.com/download_unity/47cd37c28be8/Windows64EditorInstaller/UnitySetup64-2017.4.19f1.exe"},
            {new UnityVersion("2017.4.18"), "https://download.unity3d.com/download_unity/a9236f402e28/Windows64EditorInstaller/UnitySetup64-2017.4.18f1.exe"},
            {new UnityVersion("2017.4.17"), "https://download.unity3d.com/download_unity/05307cddbb71/Windows64EditorInstaller/UnitySetup64-2017.4.17f1.exe"},
            {new UnityVersion("2017.4.16"), "https://download.unity3d.com/download_unity/7f7bdd1ef02b/Windows64EditorInstaller/UnitySetup64-2017.4.16f1.exe"},
            {new UnityVersion("2017.4.15"), "https://download.unity3d.com/download_unity/5d485b4897a7/Windows64EditorInstaller/UnitySetup64-2017.4.15f1.exe"},
            {new UnityVersion("2017.4.14"), "https://download.unity3d.com/download_unity/b28150134d55/Windows64EditorInstaller/UnitySetup64-2017.4.14f1.exe"},
            {new UnityVersion("2017.4.13"), "https://download.unity3d.com/download_unity/6902ad48015d/Windows64EditorInstaller/UnitySetup64-2017.4.13f1.exe"},
            {new UnityVersion("2017.4.12"), "https://download.unity3d.com/download_unity/b582b87345b1/Windows64EditorInstaller/UnitySetup64-2017.4.12f1.exe"},
            {new UnityVersion("2017.4.11"), "https://download.unity3d.com/download_unity/8c6b8ef6d111/Windows64EditorInstaller/UnitySetup64-2017.4.11f1.exe"},
            {new UnityVersion("2017.4.10"), "https://download.unity3d.com/download_unity/f2cce2a5991f/Windows64EditorInstaller/UnitySetup64-2017.4.10f1.exe"},
            {new UnityVersion("2017.4.9"), "https://download.unity3d.com/download_unity/6d84dfc57ccf/Windows64EditorInstaller/UnitySetup64-2017.4.9f1.exe"},
            {new UnityVersion("2017.4.8"), "https://download.unity3d.com/download_unity/5ab7f4878ef1/Windows64EditorInstaller/UnitySetup64-2017.4.8f1.exe"},
            {new UnityVersion("2017.4.7"), "https://download.unity3d.com/download_unity/de9eb5ca33c5/Windows64EditorInstaller/UnitySetup64-2017.4.7f1.exe"},
            {new UnityVersion("2017.4.6"), "https://download.unity3d.com/download_unity/c24f30193bac/Windows64EditorInstaller/UnitySetup64-2017.4.6f1.exe"},
            {new UnityVersion("2017.4.5"), "https://download.unity3d.com/download_unity/89d1db9cb682/Windows64EditorInstaller/UnitySetup64-2017.4.5f1.exe"},
            {new UnityVersion("2017.4.4"), "https://download.unity3d.com/download_unity/645c9050ba4d/Windows64EditorInstaller/UnitySetup64-2017.4.4f1.exe"},
            {new UnityVersion("2017.4.3"), "https://download.unity3d.com/download_unity/21ae32b5a9cb/Windows64EditorInstaller/UnitySetup64-2017.4.3f1.exe"},
            {new UnityVersion("2017.4.2"), "https://download.unity3d.com/download_unity/52d9cb89b362/Windows64EditorInstaller/UnitySetup64-2017.4.2f2.exe"},
            {new UnityVersion("2017.4.1"), "https://download.unity3d.com/download_unity/9231f953d9d3/Windows64EditorInstaller/UnitySetup64-2017.4.1f1.exe"},
            {new UnityVersion("2017.3.1"), "https://download.unity3d.com/download_unity/fc1d3344e6ea/Windows64EditorInstaller/UnitySetup64-2017.3.1f1.exe"},
            {new UnityVersion("2017.3.0"), "https://download.unity3d.com/download_unity/a9f86dcd79df/Windows64EditorInstaller/UnitySetup64-2017.3.0f3.exe"},
            {new UnityVersion("2017.2.5"), "https://download.unity3d.com/download_unity/588dc79c95ed/Windows64EditorInstaller/UnitySetup64-2017.2.5f1.exe"},
            {new UnityVersion("2017.2.4"), "https://download.unity3d.com/download_unity/f1557d1f61fd/Windows64EditorInstaller/UnitySetup64-2017.2.4f1.exe"},
            {new UnityVersion("2017.2.3"), "https://download.unity3d.com/download_unity/372229934efd/Windows64EditorInstaller/UnitySetup64-2017.2.3f1.exe"},
            {new UnityVersion("2017.2.2"), "https://download.unity3d.com/download_unity/1f4e0f9b6a50/Windows64EditorInstaller/UnitySetup64-2017.2.2f1.exe"},
            {new UnityVersion("2017.2.1"), "https://download.unity3d.com/download_unity/94bf3f9e6b5e/Windows64EditorInstaller/UnitySetup64-2017.2.1f1.exe"},
            {new UnityVersion("2017.2.0"), "https://download.unity3d.com/download_unity/46dda1414e51/Windows64EditorInstaller/UnitySetup64-2017.2.0f3.exe"},
            {new UnityVersion("2017.1.5"), "https://download.unity3d.com/download_unity/9758a36cfaa6/Windows64EditorInstaller/UnitySetup64-2017.1.5f1.exe"},
            {new UnityVersion("2017.1.4"), "https://download.unity3d.com/download_unity/9fd71167a288/Windows64EditorInstaller/UnitySetup64-2017.1.4f1.exe"},
            {new UnityVersion("2017.1.3"), "https://download.unity3d.com/download_unity/574eeb502d14/Windows64EditorInstaller/UnitySetup64-2017.1.3f1.exe"},
            {new UnityVersion("2017.1.2"), "https://download.unity3d.com/download_unity/cc85bf6a8a04/Windows64EditorInstaller/UnitySetup64-2017.1.2f1.exe"},
            {new UnityVersion("2017.1.1"), "https://download.unity3d.com/download_unity/5d30cf096e79/Windows64EditorInstaller/UnitySetup64-2017.1.1f1.exe"},
            {new UnityVersion("2017.1.0"), "https://download.unity3d.com/download_unity/472613c02cf7/Windows64EditorInstaller/UnitySetup64-2017.1.0f3.exe"},
            {new UnityVersion("5.6.7"), "https://download.unity3d.com/download_unity/e80cc3114ac1/Windows64EditorInstaller/UnitySetup64-5.6.7f1.exe"},
            {new UnityVersion("5.6.6"), "https://download.unity3d.com/download_unity/6bac21139588/Windows64EditorInstaller/UnitySetup64-5.6.6f2.exe"},
            {new UnityVersion("5.6.5"), "https://download.unity3d.com/download_unity/2cac56bf7bb6/Windows64EditorInstaller/UnitySetup64-5.6.5f1.exe"},
            {new UnityVersion("5.6.4"), "https://download.unity3d.com/download_unity/ac7086b8d112/Windows64EditorInstaller/UnitySetup64-5.6.4f1.exe"},
            {new UnityVersion("5.6.3"), "https://download.unity3d.com/download_unity/d3101c3b8468/Windows64EditorInstaller/UnitySetup64-5.6.3f1.exe"},
            {new UnityVersion("5.6.2"), "https://download.unity3d.com/download_unity/a2913c821e27/Windows64EditorInstaller/UnitySetup64-5.6.2f1.exe"},
            {new UnityVersion("5.6.1"), "https://download.unity3d.com/download_unity/2860b30f0b54/Windows64EditorInstaller/UnitySetup64-5.6.1f1.exe"},
            {new UnityVersion("5.6.0"), "https://download.unity3d.com/download_unity/497a0f351392/Windows64EditorInstaller/UnitySetup64-5.6.0f3.exe"},
            {new UnityVersion("5.5.6"), "https://download.unity3d.com/download_unity/3fb31a95adee/Windows64EditorInstaller/UnitySetup64-5.5.6f1.exe"},
            {new UnityVersion("5.5.5"), "https://download.unity3d.com/download_unity/d875e6967482/Windows64EditorInstaller/UnitySetup64-5.5.5f1.exe"},
            {new UnityVersion("5.5.4"), "https://download.unity3d.com/download_unity/8ffd0efd98b1/Windows64EditorInstaller/UnitySetup64-5.5.4f1.exe"},
            {new UnityVersion("5.5.3"), "https://download.unity3d.com/download_unity/4d2f809fd6f3/Windows64EditorInstaller/UnitySetup64-5.5.3f1.exe"},
            {new UnityVersion("5.5.2"), "https://download.unity3d.com/download_unity/3829d7f588f3/Windows64EditorInstaller/UnitySetup64-5.5.2f1.exe"},
            {new UnityVersion("5.5.1"), "https://download.unity3d.com/download_unity/88d00a7498cd/Windows64EditorInstaller/UnitySetup64-5.5.1f1.exe"},
            {new UnityVersion("5.5.0"), "https://download.unity3d.com/download_unity/38b4efef76f0/Windows64EditorInstaller/UnitySetup64-5.5.0f3.exe"},
            {new UnityVersion("5.4.6"), "https://download.unity3d.com/download_unity/7c5210d1343f/Windows64EditorInstaller/UnitySetup64-5.4.6f3.exe"},
            {new UnityVersion("5.4.5"), "https://download.unity3d.com/download_unity/68943b6c8c42/Windows64EditorInstaller/UnitySetup64-5.4.5f1.exe"},
            {new UnityVersion("5.4.4"), "https://download.unity3d.com/download_unity/5a3967d8c55d/Windows64EditorInstaller/UnitySetup64-5.4.4f1.exe"},
            {new UnityVersion("5.4.3"), "https://download.unity3d.com/download_unity/01f4c123905a/Windows64EditorInstaller/UnitySetup64-5.4.3f1.exe"},
            {new UnityVersion("5.4.2"), "https://download.unity3d.com/download_unity/b7e030c65c9b/Windows64EditorInstaller/UnitySetup64-5.4.2f2.exe"},
            {new UnityVersion("5.4.1"), "https://download.unity3d.com/download_unity/649f48bbbf0f/Windows64EditorInstaller/UnitySetup64-5.4.1f1.exe"},
            {new UnityVersion("5.4.0"), "https://download.unity3d.com/download_unity/a6d8d714de6f/Windows64EditorInstaller/UnitySetup64-5.4.0f3.exe"},
            {new UnityVersion("5.3.8"), "https://download.unity3d.com/download_unity/0c7e33ff9c0e/Windows64EditorInstaller/UnitySetup64-5.3.8f2.exe"},
            {new UnityVersion("5.3.7"), "https://download.unity3d.com/download_unity/c347874230fb/Windows64EditorInstaller/UnitySetup64-5.3.7f1.exe"},
            {new UnityVersion("5.3.6"), "https://download.unity3d.com/download_unity/29055738eb78/Windows64EditorInstaller/UnitySetup64-5.3.6f1.exe"},
            {new UnityVersion("5.3.5"), "https://download.unity3d.com/download_unity/960ebf59018a/Windows64EditorInstaller/UnitySetup64-5.3.5f1.exe"},
            {new UnityVersion("5.3.4"), "https://download.unity3d.com/download_unity/fdbb5133b820/Windows64EditorInstaller/UnitySetup64-5.3.4f1.exe"},
            {new UnityVersion("5.3.3"), "https://download.unity3d.com/download_unity/910d71450a97/Windows64EditorInstaller/UnitySetup64-5.3.3f1.exe"},
            {new UnityVersion("5.3.2"), "https://download.unity3d.com/download_unity/e87ab445ead0/Windows64EditorInstaller/UnitySetup64-5.3.2f1.exe"},
            {new UnityVersion("5.3.1"), "https://download.unity3d.com/download_unity/cc9cbbcc37b4/Windows64EditorInstaller/UnitySetup64-5.3.1f1.exe"},
            {new UnityVersion("5.3.0"), "https://download.unity3d.com/download_unity/2524e04062b4/Windows64EditorInstaller/UnitySetup64-5.3.0f4.exe"},
            {new UnityVersion("5.2.5"), "https://download.unity3d.com/download_unity/ad2d0368e248/Windows64EditorInstaller/UnitySetup64-5.2.5f1.exe"},
            {new UnityVersion("5.2.4"), "https://download.unity3d.com/download_unity/98095704e6fe/Windows64EditorInstaller/UnitySetup64-5.2.4f1.exe"},
            {new UnityVersion("5.2.3"), "https://download.unity3d.com/download_unity/f3d16a1fa2dd/Windows64EditorInstaller/UnitySetup64-5.2.3f1.exe"},
            {new UnityVersion("5.2.2"), "https://download.unity3d.com/download_unity/3757309da7e7/Windows64EditorInstaller/UnitySetup64-5.2.2f1.exe"},
            {new UnityVersion("5.2.1"), "https://download.unity3d.com/download_unity/44735ea161b3/Windows64EditorInstaller/UnitySetup64-5.2.1f1.exe"},
            {new UnityVersion("5.2.0"), "https://download.unity3d.com/download_unity/e7947df39b5c/Windows64EditorInstaller/UnitySetup64-5.2.0f3.exe"},
            {new UnityVersion("5.1.5"), "https://download.unity3d.com/download_unity/9de525f1a6a8/Windows64EditorInstaller/UnitySetup64-5.1.5f1.exe"},
            {new UnityVersion("5.1.4"), "https://download.unity3d.com/download_unity/36d0f3617432/Windows64EditorInstaller/UnitySetup64-5.1.4f1.exe"},
            {new UnityVersion("5.1.3"), "https://download.unity3d.com/download_unity/b0a23b31c3d8/Windows64EditorInstaller/UnitySetup64-5.1.3f1.exe"},
            {new UnityVersion("5.1.2"), "https://download.unity3d.com/download_unity/afd2369b692a/Windows64EditorInstaller/UnitySetup64-5.1.2f1.exe"},
            {new UnityVersion("5.1.1"), "https://download.unity3d.com/download_unity/2046fc06d4d8/Windows64EditorInstaller/UnitySetup64-5.1.1f1.exe"},
            {new UnityVersion("5.1.0"), "https://download.unity3d.com/download_unity/ec70b008569d/Windows64EditorInstaller/UnitySetup64-5.1.0f3.exe"},
            {new UnityVersion("5.0.4"), "https://download.unity3d.com/download_unity/1d75c08f1c9c/Windows64EditorInstaller/UnitySetup64-5.0.4f1.exe"},
            {new UnityVersion("5.0.3"), "https://download.unity3d.com/download_unity/c28c7860811c/Windows64EditorInstaller/UnitySetup64-5.0.3f2.exe"},
            {new UnityVersion("5.0.2"), "https://download.unity3d.com/download_unity/0b02744d4013/Windows64EditorInstaller/UnitySetup64-5.0.2f1.exe"},
            {new UnityVersion("5.0.1"), "https://download.unity3d.com/download_unity/5a2e8fe35a68/Windows64EditorInstaller/UnitySetup64-5.0.1f1.exe"},
            {new UnityVersion("5.0.0"), "https://download.unity3d.com/download_unity/5b98b70ebeb9/Windows64EditorInstaller/UnitySetup64-5.0.0f4.exe"},
            {new UnityVersion("4.7.2"), "https://download.unity3d.com/download_unity/UnitySetup-4.7.2.exe"},
            {new UnityVersion("4.7.1"), "https://download.unity3d.com/download_unity/UnitySetup-4.7.1.exe"},
            {new UnityVersion("4.7.0"), "https://beta.unity3d.com/download/3733150244/UnitySetup-4.7.0.exe"},
            {new UnityVersion("4.6.9"), "https://beta.unity3d.com/download/7083239589/UnitySetup-4.6.9.exe"},
            {new UnityVersion("4.6.8"), "https://download.unity3d.com/download_unity/UnitySetup-4.6.8.exe"},
            {new UnityVersion("4.6.7"), "https://download.unity3d.com/download_unity/UnitySetup-4.6.7.exe"},
            {new UnityVersion("4.6.6"), "https://download.unity3d.com/download_unity/UnitySetup-4.6.6.exe"},
            {new UnityVersion("4.6.5"), "https://download.unity3d.com/download_unity/UnitySetup-4.6.5.exe"},
            {new UnityVersion("4.6.4"), "https://download.unity3d.com/download_unity/UnitySetup-4.6.4.exe"},
            {new UnityVersion("4.6.3"), "https://download.unity3d.com/download_unity/UnitySetup-4.6.3.exe"},
            {new UnityVersion("4.6.2"), "https://download.unity3d.com/download_unity/UnitySetup-4.6.2.exe"},
            {new UnityVersion("4.6.1"), "https://download.unity3d.com/download_unity/UnitySetup-4.6.1.exe"},
            {new UnityVersion("4.6.0"), "https://download.unity3d.com/download_unity/UnitySetup-4.6.0.exe"},
            {new UnityVersion("4.5.5"), "https://download.unity3d.com/download_unity/UnitySetup-4.5.5.exe"},
            {new UnityVersion("4.5.4"), "https://download.unity3d.com/download_unity/UnitySetup-4.5.4.exe"},
            {new UnityVersion("4.5.3"), "https://download.unity3d.com/download_unity/UnitySetup-4.5.3.exe"},
            {new UnityVersion("4.5.2"), "https://download.unity3d.com/download_unity/UnitySetup-4.5.2.exe"},
            {new UnityVersion("4.5.1"), "https://download.unity3d.com/download_unity/UnitySetup-4.5.1.exe"},
            {new UnityVersion("4.5.0"), "https://download.unity3d.com/download_unity/UnitySetup-4.5.0.exe"},
            {new UnityVersion("4.3.4"), "https://download.unity3d.com/download_unity/UnitySetup-4.3.4.exe"},
            {new UnityVersion("4.3.3"), "https://download.unity3d.com/download_unity/UnitySetup-4.3.3.exe"},
            {new UnityVersion("4.3.2"), "https://download.unity3d.com/download_unity/UnitySetup-4.3.2.exe"},
            {new UnityVersion("4.3.1"), "https://download.unity3d.com/download_unity/UnitySetup-4.3.1.exe"},
            {new UnityVersion("4.3.0"), "https://download.unity3d.com/download_unity/UnitySetup-4.3.0.exe"},
            {new UnityVersion("4.2.2"), "https://download.unity3d.com/download_unity/UnitySetup-4.2.2.exe"},
            {new UnityVersion("4.2.1"), "https://download.unity3d.com/download_unity/UnitySetup-4.2.1.exe"},
            {new UnityVersion("4.2.0"), "https://download.unity3d.com/download_unity/UnitySetup-4.2.0.exe"},
            {new UnityVersion("4.1.5"), "https://download.unity3d.com/download_unity/UnitySetup-4.1.5.exe"},
            {new UnityVersion("4.1.4"), "https://download.unity3d.com/download_unity/UnitySetup-4.1.4.exe"},
            {new UnityVersion("4.1.3"), "https://download.unity3d.com/download_unity/UnitySetup-4.1.3.exe"},
            {new UnityVersion("4.1.2"), "https://download.unity3d.com/download_unity/UnitySetup-4.1.2.exe"},
            {new UnityVersion("4.1.0"), "https://download.unity3d.com/download_unity/UnitySetup-4.1.0.exe"},
            {new UnityVersion("4.0.1"), "https://download.unity3d.com/download_unity/UnitySetup-4.0.1.exe"},
            {new UnityVersion("4.0.0"), "https://download.unity3d.com/download_unity/UnitySetup-4.0.0.exe"},
            {new UnityVersion("3.5.7"), "https://download.unity3d.com/download_unity/UnitySetup-3.5.7.exe"},
            {new UnityVersion("3.5.6"), "https://download.unity3d.com/download_unity/UnitySetup-3.5.6.exe"},
            {new UnityVersion("3.5.5"), "https://download.unity3d.com/download_unity/UnitySetup-3.5.5.exe"},
            {new UnityVersion("3.5.4"), "https://download.unity3d.com/download_unity/UnitySetup-3.5.4.exe"},
            {new UnityVersion("3.5.3"), "https://download.unity3d.com/download_unity/UnitySetup-3.5.3.exe"},
            {new UnityVersion("3.5.2"), "https://download.unity3d.com/download_unity/UnitySetup-3.5.2.exe"},
            {new UnityVersion("3.5.1"), "https://download.unity3d.com/download_unity/UnitySetup-3.5.1.exe"},
            {new UnityVersion("3.5.0"), "https://download.unity3d.com/download_unity/UnitySetup-3.5.0.exe"},
            {new UnityVersion("3.4.2"), "https://download.unity3d.com/download_unity/UnitySetup-3.4.2.exe"},
            {new UnityVersion("3.4.1"), "https://download.unity3d.com/download_unity/UnitySetup-3.4.1.exe"},
            {new UnityVersion("3.4.0"), "https://download.unity3d.com/download_unity/UnitySetup-3.4.0.exe"}
        };

        private static Dictionary<UnityVersion, string> MacDownloadLinks = new Dictionary<UnityVersion, string>()
        {
            {new UnityVersion("2021.1.21"), "https://download.unity3d.com/download_unity/f2d5d3c59f8c/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.20"), "https://download.unity3d.com/download_unity/be552157821d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.19"), "https://download.unity3d.com/download_unity/5f5eb8bbdc25/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.18"), "https://download.unity3d.com/download_unity/25bdc3efbc2d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.17"), "https://download.unity3d.com/download_unity/03b40fe07a36/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.16"), "https://download.unity3d.com/download_unity/5fa502fca597/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.15"), "https://download.unity3d.com/download_unity/e767a7370072/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.14"), "https://download.unity3d.com/download_unity/51d2f824827f/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.13"), "https://download.unity3d.com/download_unity/a03098edbbe0/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.12"), "https://download.unity3d.com/download_unity/afcadd793de6/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.11"), "https://download.unity3d.com/download_unity/4d8c25f7477e/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.10"), "https://download.unity3d.com/download_unity/b15f561b2cef/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.9"), "https://download.unity3d.com/download_unity/7a790e367ab3/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.7"), "https://download.unity3d.com/download_unity/d91830b65d9b/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.6"), "https://download.unity3d.com/download_unity/c0fade0cc7e9/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.5"), "https://download.unity3d.com/download_unity/3737af19df53/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.4"), "https://download.unity3d.com/download_unity/4cd64a618c1b/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.3"), "https://download.unity3d.com/download_unity/4bef613afd59/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.2"), "https://download.unity3d.com/download_unity/e5d502d80fbb/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.1"), "https://download.unity3d.com/download_unity/6fdc41dfa55a/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2021.1.0"), "https://download.unity3d.com/download_unity/61a549675243/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.18"), "https://download.unity3d.com/download_unity/a7d1c678663c/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.17"), "https://download.unity3d.com/download_unity/a4537701e4ab/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.16"), "https://download.unity3d.com/download_unity/049d6eca3c44/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.15"), "https://download.unity3d.com/download_unity/6cf78cb77498/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.14"), "https://download.unity3d.com/download_unity/d0d1bb862f9d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.13"), "https://download.unity3d.com/download_unity/71691879b7f5/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.12"), "https://download.unity3d.com/download_unity/b3b2c6512326/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.11"), "https://download.unity3d.com/download_unity/99c7afb366b3/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.10"), "https://download.unity3d.com/download_unity/297d780c91bc/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.9"), "https://download.unity3d.com/download_unity/108be757e447/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.8"), "https://download.unity3d.com/download_unity/507919d4fff5/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.7"), "https://download.unity3d.com/download_unity/dd97f2c94397/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.6"), "https://download.unity3d.com/download_unity/338bb68529b2/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.5"), "https://download.unity3d.com/download_unity/8095aa901b9b/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.4"), "https://download.unity3d.com/download_unity/0abb6314276a/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.3"), "https://download.unity3d.com/download_unity/76626098c1c4/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.2"), "https://download.unity3d.com/download_unity/8fd9074bf66c/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.1"), "https://download.unity3d.com/download_unity/77a89f25062f/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.3.0"), "https://download.unity3d.com/download_unity/c7b5465681fb/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.2.7"), "https://download.unity3d.com/download_unity/c53830e277f1/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.2.6"), "https://download.unity3d.com/download_unity/8a2143876886/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.2.5"), "https://download.unity3d.com/download_unity/e2c53f129de5/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.2.4"), "https://download.unity3d.com/download_unity/becced5a802b/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.2.3"), "https://download.unity3d.com/download_unity/8ff31bc5bf5b/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.2.2"), "https://download.unity3d.com/download_unity/068178b99f32/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.2.1"), "https://download.unity3d.com/download_unity/270dd8c3da1c/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.2.0"), "https://download.unity3d.com/download_unity/3721df5a8b28/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.17"), "https://download.unity3d.com/download_unity/9957aee8edc2/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.16"), "https://download.unity3d.com/download_unity/f483ad6465d6/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.15"), "https://download.unity3d.com/download_unity/97d0ae02d19d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.14"), "https://download.unity3d.com/download_unity/d81f64f5201d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.13"), "https://download.unity3d.com/download_unity/5e24f28bfbc0/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.12"), "https://download.unity3d.com/download_unity/55b56f0a86e3/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.11"), "https://download.unity3d.com/download_unity/698c1113cef0/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.10"), "https://download.unity3d.com/download_unity/974a9d56f159/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.9"), "https://download.unity3d.com/download_unity/145f5172610f/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.8"), "https://download.unity3d.com/download_unity/22e8c0b0c3ec/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.7"), "https://download.unity3d.com/download_unity/064ffcdb64ad/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.6"), "https://download.unity3d.com/download_unity/fc477ca6df10/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.5"), "https://download.unity3d.com/download_unity/e025938fdedc/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.4"), "https://download.unity3d.com/download_unity/fa717bb873ec/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.3"), "https://download.unity3d.com/download_unity/cf5c4788e1d8/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.2"), "https://download.unity3d.com/download_unity/7b32bc54ba47/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.1"), "https://download.unity3d.com/download_unity/2285c3239188/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2020.1.0"), "https://beta.unity3d.com/download/2ab9c4179772/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.30"), "https://download.unity3d.com/download_unity/e8c891080a1f/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.29"), "https://download.unity3d.com/download_unity/0eeae20b1d82/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.28"), "https://download.unity3d.com/download_unity/1381962e9d08/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.27"), "https://download.unity3d.com/download_unity/23dc10685eb4/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.26"), "https://download.unity3d.com/download_unity/e0392c6b2363/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.25"), "https://download.unity3d.com/download_unity/01a0494af254/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.24"), "https://download.unity3d.com/download_unity/5da6f0345e82/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.23"), "https://download.unity3d.com/download_unity/3f4e01f1a5ec/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.22"), "https://download.unity3d.com/download_unity/9fdda2fe27ad/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.21"), "https://download.unity3d.com/download_unity/b76dac84db26/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.20"), "https://download.unity3d.com/download_unity/6dd1c08eedfa/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.19"), "https://download.unity3d.com/download_unity/ca5b14067cec/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.18"), "https://download.unity3d.com/download_unity/3310a4d4f880/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.17"), "https://download.unity3d.com/download_unity/667c8606c536/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.16"), "https://download.unity3d.com/download_unity/e05b6e02d63e/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.15"), "https://download.unity3d.com/download_unity/fbf367ac14e9/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.14"), "https://download.unity3d.com/download_unity/4037e52648cd/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.13"), "https://download.unity3d.com/download_unity/518737b1de84/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.12"), "https://download.unity3d.com/download_unity/225e826a680e/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.11"), "https://download.unity3d.com/download_unity/2d9804dddde7/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.10"), "https://download.unity3d.com/download_unity/5311b3af6f69/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.9"), "https://download.unity3d.com/download_unity/50fe8a171dd9/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.8"), "https://download.unity3d.com/download_unity/60781d942082/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.7"), "https://download.unity3d.com/download_unity/e992b1a16e65/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.6"), "https://download.unity3d.com/download_unity/a7aea80e3716/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.5"), "https://download.unity3d.com/download_unity/81610f64359c/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.4"), "https://download.unity3d.com/download_unity/1f1dac67805b/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.3"), "https://download.unity3d.com/download_unity/f880dceab6fe/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.2"), "https://download.unity3d.com/download_unity/20b4642a3455/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.1"), "https://download.unity3d.com/download_unity/e6c045e14e4e/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.4.0"), "https://download.unity3d.com/download_unity/0af376155913/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.15"), "https://download.unity3d.com/download_unity/59ff3e03856d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.14"), "https://download.unity3d.com/download_unity/2b330bf6d2d8/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.13"), "https://download.unity3d.com/download_unity/d4ddf0d95db9/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.12"), "https://download.unity3d.com/download_unity/84b23722532d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.11"), "https://download.unity3d.com/download_unity/ceef2d848e70/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.10"), "https://download.unity3d.com/download_unity/5968d7f82152/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.9"), "https://download.unity3d.com/download_unity/e6e740a1c473/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.8"), "https://download.unity3d.com/download_unity/4ba98e9386ed/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.7"), "https://download.unity3d.com/download_unity/6437fd74d35d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.6"), "https://download.unity3d.com/download_unity/5c3fb0a11183/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.5"), "https://download.unity3d.com/download_unity/d691e07d38ef/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.4"), "https://download.unity3d.com/download_unity/4f139db2fdbd/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.3"), "https://download.unity3d.com/download_unity/7ceaae5f7503/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.2"), "https://download.unity3d.com/download_unity/c46a3a38511e/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.1"), "https://download.unity3d.com/download_unity/89d6087839c2/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.3.0"), "https://download.unity3d.com/download_unity/27ab2135bccf/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.21"), "https://download.unity3d.com/download_unity/9d528d026557/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.20"), "https://download.unity3d.com/download_unity/c67d00285037/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.19"), "https://download.unity3d.com/download_unity/929ab4d01772/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.18"), "https://download.unity3d.com/download_unity/bbf64de26e34/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.17"), "https://download.unity3d.com/download_unity/8e603399ca02/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.16"), "https://download.unity3d.com/download_unity/b9898e2d04a4/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.15"), "https://download.unity3d.com/download_unity/dcb72c2e9334/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.14"), "https://download.unity3d.com/download_unity/49dd4e9fa428/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.13"), "https://download.unity3d.com/download_unity/e20f6c7e5017/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.12"), "https://download.unity3d.com/download_unity/b1a7e1fb4fa5/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.11"), "https://beta.unity3d.com/download/5f859a4cfee5/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.10"), "https://download.unity3d.com/download_unity/923acd2d43aa/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.9"), "https://download.unity3d.com/download_unity/ebce4d76e6e8/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.8"), "https://download.unity3d.com/download_unity/ff5b465c8d13/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.7"), "https://download.unity3d.com/download_unity/c96f78eb5904/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.6"), "https://download.unity3d.com/download_unity/fe82a0e88406/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.5"), "https://beta.unity3d.com/download/9dace1eed4cc/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.4"), "https://download.unity3d.com/download_unity/c63b2af89a85/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.3"), "https://download.unity3d.com/download_unity/8e55c27a4621/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.2"), "https://download.unity3d.com/download_unity/ab112815d860/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.1"), "https://download.unity3d.com/download_unity/ca4d5af0be6f/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.2.0"), "https://download.unity3d.com/download_unity/20c1667945cf/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.14"), "https://download.unity3d.com/download_unity/148b5891095a/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.13"), "https://download.unity3d.com/download_unity/b5956c0a61e7/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.12"), "https://download.unity3d.com/download_unity/f04f5427219e/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.11"), "https://download.unity3d.com/download_unity/9b001d489a54/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.10"), "https://download.unity3d.com/download_unity/f007ed779b7a/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.9"), "https://download.unity3d.com/download_unity/d5f1b37da199/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.8"), "https://download.unity3d.com/download_unity/7938dd008a75/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.7"), "https://download.unity3d.com/download_unity/f3c4928e5742/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.6"), "https://download.unity3d.com/download_unity/f2970305fe1c/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.5"), "https://download.unity3d.com/download_unity/0ca0f5646614/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.4"), "https://download.unity3d.com/download_unity/ffa3a7a2dd7d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.3"), "https://download.unity3d.com/download_unity/dc414eb9ed43/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.2"), "https://download.unity3d.com/download_unity/3e18427e571f/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.1"), "https://download.unity3d.com/download_unity/fef62e97e63b/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2019.1.0"), "https://download.unity3d.com/download_unity/292b93d75a2c/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.36"), "https://download.unity3d.com/download_unity/6cd387d23174/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.35"), "https://download.unity3d.com/download_unity/dbb5675dce2d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.34"), "https://download.unity3d.com/download_unity/ae2afac172fb/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.33"), "https://download.unity3d.com/download_unity/d75f7e9df24c/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.32"), "https://download.unity3d.com/download_unity/fba45da84107/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.31"), "https://download.unity3d.com/download_unity/212ea663d844/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.30"), "https://download.unity3d.com/download_unity/c698a062d8e6/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.29"), "https://download.unity3d.com/download_unity/50cce2edf27f/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.28"), "https://download.unity3d.com/download_unity/a2d4f71491a4/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.27"), "https://download.unity3d.com/download_unity/4e283b7d3f88/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.26"), "https://download.unity3d.com/download_unity/a7ac1c6396db/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.25"), "https://download.unity3d.com/download_unity/b07bfa0a8827/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.24"), "https://download.unity3d.com/download_unity/3071911a89e9/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.23"), "https://download.unity3d.com/download_unity/c9cf1a90e812/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.22"), "https://download.unity3d.com/download_unity/3362ffbb7aa1/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.21"), "https://download.unity3d.com/download_unity/fd3915227633/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.20"), "https://download.unity3d.com/download_unity/008688490035/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.19"), "https://download.unity3d.com/download_unity/459f70f82ea4/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.18"), "https://download.unity3d.com/download_unity/61fce66342ad/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.17"), "https://beta.unity3d.com/download/b830f56f42f0/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.16"), "https://beta.unity3d.com/download/e6e9ca02b32a/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.15"), "https://download.unity3d.com/download_unity/13f5a1bf9ca1/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.14"), "https://download.unity3d.com/download_unity/05119b33d0b7/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.13"), "https://download.unity3d.com/download_unity/497f083a43af/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.12"), "https://download.unity3d.com/download_unity/59ddc4c59b4f/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.11"), "https://download.unity3d.com/download_unity/7098af2f11ea/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.10"), "https://download.unity3d.com/download_unity/a0470569e97b/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.9"), "https://download.unity3d.com/download_unity/ca372476eaba/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.8"), "https://download.unity3d.com/download_unity/9bc9d983d803/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.7"), "https://download.unity3d.com/download_unity/b9a993fd1334/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.6"), "https://download.unity3d.com/download_unity/cde1bbcc9f0d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.5"), "https://download.unity3d.com/download_unity/7b38f8ac282e/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.4"), "https://download.unity3d.com/download_unity/5440768ff61c/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.3"), "https://download.unity3d.com/download_unity/8a9509a5aff9/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.2"), "https://download.unity3d.com/download_unity/d6fb3630ea75/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.1"), "https://download.unity3d.com/download_unity/b7c424a951c0/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.4.0"), "https://download.unity3d.com/download_unity/b6ffa8986c8d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.14"), "https://download.unity3d.com/download_unity/d0e9f15437b1/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.13"), "https://download.unity3d.com/download_unity/06548a9e9582/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.12"), "https://download.unity3d.com/download_unity/8afd630d1f5b/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.11"), "https://download.unity3d.com/download_unity/5063218e4ab8/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.10"), "https://download.unity3d.com/download_unity/f88de2c96e63/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.9"), "https://download.unity3d.com/download_unity/947e1ea5aa8d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.8"), "https://download.unity3d.com/download_unity/fc0fe30d6d91/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.7"), "https://download.unity3d.com/download_unity/9e14d22a41bb/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.6"), "https://download.unity3d.com/download_unity/a220877bc173/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.5"), "https://download.unity3d.com/download_unity/76b3e37670a4/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.4"), "https://download.unity3d.com/download_unity/1d952368ca3a/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.3"), "https://download.unity3d.com/download_unity/393bae82dbb8/MacEditorInstaller/Unity-2018.3.3f1.pkg"},
            {new UnityVersion("2018.3.2"), "https://download.unity3d.com/download_unity/b3c100a4b73a/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.1"), "https://download.unity3d.com/download_unity/bb579dc42f1d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.3.0"), "https://download.unity3d.com/download_unity/6e9a27477296/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.2.21"), "https://download.unity3d.com/download_unity/a122f5dc316d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2018.2.20"), "https://download.unity3d.com/download_unity/cef3e6c0c622/MacEditorInstaller/Unity-2018.2.20f1.pkg"},
            {new UnityVersion("2018.2.19"), "https://download.unity3d.com/download_unity/06990f28ba00/MacEditorInstaller/Unity-2018.2.19f1.pkg"},
            {new UnityVersion("2018.2.18"), "https://download.unity3d.com/download_unity/4550892b6062/MacEditorInstaller/Unity-2018.2.18f1.pkg"},
            {new UnityVersion("2018.2.17"), "https://download.unity3d.com/download_unity/88933597c842/MacEditorInstaller/Unity-2018.2.17f1.pkg"},
            {new UnityVersion("2018.2.16"), "https://download.unity3d.com/download_unity/39a4ac3d51f6/MacEditorInstaller/Unity-2018.2.16f1.pkg"},
            {new UnityVersion("2018.2.15"), "https://download.unity3d.com/download_unity/65e0713a5949/MacEditorInstaller/Unity-2018.2.15f1.pkg"},
            {new UnityVersion("2018.2.14"), "https://download.unity3d.com/download_unity/3262fb3b0716/MacEditorInstaller/Unity-2018.2.14f1.pkg"},
            {new UnityVersion("2018.2.13"), "https://download.unity3d.com/download_unity/83fbdcd35118/MacEditorInstaller/Unity-2018.2.13f1.pkg"},
            {new UnityVersion("2018.2.12"), "https://download.unity3d.com/download_unity/0a46ddfcfad4/MacEditorInstaller/Unity-2018.2.12f1.pkg"},
            {new UnityVersion("2018.2.11"), "https://download.unity3d.com/download_unity/38bd7dec5000/MacEditorInstaller/Unity-2018.2.11f1.pkg"},
            {new UnityVersion("2018.2.10"), "https://download.unity3d.com/download_unity/674aa5a67ed5/MacEditorInstaller/Unity-2018.2.10f1.pkg"},
            {new UnityVersion("2018.2.9"), "https://download.unity3d.com/download_unity/2207421190e9/MacEditorInstaller/Unity-2018.2.9f1.pkg"},
            {new UnityVersion("2018.2.8"), "https://download.unity3d.com/download_unity/ae1180820377/MacEditorInstaller/Unity-2018.2.8f1.pkg"},
            {new UnityVersion("2018.2.7"), "https://download.unity3d.com/download_unity/4ebd28dd9664/MacEditorInstaller/Unity-2018.2.7f1.pkg"},
            {new UnityVersion("2018.2.6"), "https://download.unity3d.com/download_unity/c591d9a97a0b/MacEditorInstaller/Unity-2018.2.6f1.pkg"},
            {new UnityVersion("2018.2.5"), "https://download.unity3d.com/download_unity/3071d1717b71/MacEditorInstaller/Unity-2018.2.5f1.pkg"},
            {new UnityVersion("2018.2.4"), "https://download.unity3d.com/download_unity/cb262d9ddeaf/MacEditorInstaller/Unity-2018.2.4f1.pkg"},
            {new UnityVersion("2018.2.3"), "https://download.unity3d.com/download_unity/1431a7d2ced7/MacEditorInstaller/Unity-2018.2.3f1.pkg"},
            {new UnityVersion("2018.2.2"), "https://download.unity3d.com/download_unity/c18cef34cbcd/MacEditorInstaller/Unity-2018.2.2f1.pkg"},
            {new UnityVersion("2018.2.1"), "https://download.unity3d.com/download_unity/1a9968d9f99c/MacEditorInstaller/Unity-2018.2.1f1.pkg"},
            {new UnityVersion("2018.2.0"), "https://download.unity3d.com/download_unity/787658998520/UnityDownloadAssistant-2018.2.0f2.dmg"},
            {new UnityVersion("2018.1.9"), "https://download.unity3d.com/download_unity/a6cc294b73ee/MacEditorInstaller/Unity-2018.1.9f2.pkg"},
            {new UnityVersion("2018.1.8"), "https://download.unity3d.com/download_unity/26051d4de9e9/MacEditorInstaller/Unity-2018.1.8f1.pkg"},
            {new UnityVersion("2018.1.7"), "https://download.unity3d.com/download_unity/4cb482063d12/MacEditorInstaller/Unity-2018.1.7f1.pkg"},
            {new UnityVersion("2018.1.6"), "https://download.unity3d.com/download_unity/57cc34175ccf/MacEditorInstaller/Unity-2018.1.6f1.pkg"},
            {new UnityVersion("2018.1.5"), "https://download.unity3d.com/download_unity/732dbf75922d/MacEditorInstaller/Unity-2018.1.5f1.pkg"},
            {new UnityVersion("2018.1.4"), "https://download.unity3d.com/download_unity/1a308f4ebef1/MacEditorInstaller/Unity-2018.1.4f1.pkg"},
            {new UnityVersion("2018.1.3"), "https://download.unity3d.com/download_unity/a53ad04f7c7f/MacEditorInstaller/Unity-2018.1.3f1.pkg"},
            {new UnityVersion("2018.1.2"), "https://download.unity3d.com/download_unity/a46d718d282d/MacEditorInstaller/Unity-2018.1.2f1.pkg"},
            {new UnityVersion("2018.1.1"), "https://download.unity3d.com/download_unity/b8cbb5de9840/MacEditorInstaller/Unity-2018.1.1f1.pkg"},
            {new UnityVersion("2018.1.0"), "https://download.unity3d.com/download_unity/d4d99f31acba/MacEditorInstaller/Unity-2018.1.0f2.pkg"},
            {new UnityVersion("2017.4.40"), "https://download.unity3d.com/download_unity/6e14067f8a9a/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.39"), "https://download.unity3d.com/download_unity/947131c5be7e/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.38"), "https://download.unity3d.com/download_unity/82ac2fb100ce/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.37"), "https://download.unity3d.com/download_unity/78b69503ebc4/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.36"), "https://download.unity3d.com/download_unity/c663def8414c/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.35"), "https://download.unity3d.com/download_unity/e57a7bcbbf0b/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.34"), "https://download.unity3d.com/download_unity/121f18246307/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.33"), "https://download.unity3d.com/download_unity/a8557a619e24/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.32"), "https://download.unity3d.com/download_unity/4da3ed968770/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.31"), "https://download.unity3d.com/download_unity/9c8dbc3421cb/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.30"), "https://download.unity3d.com/download_unity/c6fa43736cae/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.29"), "https://download.unity3d.com/download_unity/06508aa14ca1/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.28"), "https://download.unity3d.com/download_unity/e3a0f7dd2097/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.27"), "https://download.unity3d.com/download_unity/0c4b856e4c6e/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.26"), "https://download.unity3d.com/download_unity/3b349d10f010/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.25"), "https://download.unity3d.com/download_unity/9cba1c3a94f1/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.24"), "https://download.unity3d.com/download_unity/786769fc3439/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.23"), "https://download.unity3d.com/download_unity/f80c8a98b1b5/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.22"), "https://download.unity3d.com/download_unity/eb4bc6fa7f1d/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.21"), "https://download.unity3d.com/download_unity/de35fe252486/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.20"), "https://download.unity3d.com/download_unity/413dbd19b6dc/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.19"), "https://download.unity3d.com/download_unity/47cd37c28be8/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.18"), "https://download.unity3d.com/download_unity/a9236f402e28/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.17"), "http://beta.unity3d.com/download/05307cddbb71/UnityDownloadAssistant.dmg"},
            {new UnityVersion("2017.4.16"), "https://download.unity3d.com/download_unity/7f7bdd1ef02b/MacEditorInstaller/Unity-2017.4.16f1.pkg"},
            {new UnityVersion("2017.4.15"), "https://download.unity3d.com/download_unity/5d485b4897a7/MacEditorInstaller/Unity-2017.4.15f1.pkg"},
            {new UnityVersion("2017.4.14"), "https://download.unity3d.com/download_unity/b28150134d55/MacEditorInstaller/Unity-2017.4.14f1.pkg"},
            {new UnityVersion("2017.4.13"), "https://download.unity3d.com/download_unity/6902ad48015d/MacEditorInstaller/Unity-2017.4.13f1.pkg"},
            {new UnityVersion("2017.4.12"), "https://download.unity3d.com/download_unity/b582b87345b1/MacEditorInstaller/Unity-2017.4.12f1.pkg"},
            {new UnityVersion("2017.4.11"), "https://download.unity3d.com/download_unity/8c6b8ef6d111/MacEditorInstaller/Unity-2017.4.11f1.pkg"},
            {new UnityVersion("2017.4.10"), "https://download.unity3d.com/download_unity/f2cce2a5991f/MacEditorInstaller/Unity-2017.4.10f1.pkg"},
            {new UnityVersion("2017.4.9"), "https://download.unity3d.com/download_unity/6d84dfc57ccf/MacEditorInstaller/Unity-2017.4.9f1.pkg"},
            {new UnityVersion("2017.4.8"), "https://download.unity3d.com/download_unity/5ab7f4878ef1/MacEditorInstaller/Unity-2017.4.8f1.pkg"},
            {new UnityVersion("2017.4.7"), "https://download.unity3d.com/download_unity/de9eb5ca33c5/MacEditorInstaller/Unity-2017.4.7f1.pkg"},
            {new UnityVersion("2017.4.6"), "https://download.unity3d.com/download_unity/c24f30193bac/MacEditorInstaller/Unity-2017.4.6f1.pkg"},
            {new UnityVersion("2017.4.5"), "https://download.unity3d.com/download_unity/89d1db9cb682/MacEditorInstaller/Unity-2017.4.5f1.pkg"},
            {new UnityVersion("2017.4.4"), "https://download.unity3d.com/download_unity/645c9050ba4d/MacEditorInstaller/Unity-2017.4.4f1.pkg"},
            {new UnityVersion("2017.4.3"), "https://download.unity3d.com/download_unity/21ae32b5a9cb/MacEditorInstaller/Unity-2017.4.3f1.pkg"},
            {new UnityVersion("2017.4.2"), "https://download.unity3d.com/download_unity/52d9cb89b362/MacEditorInstaller/Unity-2017.4.2f2.pkg"},
            {new UnityVersion("2017.4.1"), "https://download.unity3d.com/download_unity/9231f953d9d3/MacEditorInstaller/Unity-2017.4.1f1.pkg"},
            {new UnityVersion("2017.3.1"), "https://download.unity3d.com/download_unity/fc1d3344e6ea/MacEditorInstaller/Unity-2017.3.1f1.pkg"},
            {new UnityVersion("2017.3.0"), "https://download.unity3d.com/download_unity/a9f86dcd79df/MacEditorInstaller/Unity-2017.3.0f3.pkg"},
            {new UnityVersion("2017.2.5"), "https://download.unity3d.com/download_unity/588dc79c95ed/MacEditorInstaller/Unity-2017.2.5f1.pkg"},
            {new UnityVersion("2017.2.4"), "https://download.unity3d.com/download_unity/f1557d1f61fd/MacEditorInstaller/Unity-2017.2.4f1.pkg"},
            {new UnityVersion("2017.2.3"), "https://download.unity3d.com/download_unity/372229934efd/MacEditorInstaller/Unity-2017.2.3f1.pkg"},
            {new UnityVersion("2017.2.2"), "https://download.unity3d.com/download_unity/1f4e0f9b6a50/MacEditorInstaller/Unity-2017.2.2f1.pkg"},
            {new UnityVersion("2017.2.1"), "https://download.unity3d.com/download_unity/94bf3f9e6b5e/MacEditorInstaller/Unity-2017.2.1f1.pkg"},
            {new UnityVersion("2017.2.0"), "https://download.unity3d.com/download_unity/46dda1414e51/MacEditorInstaller/Unity-2017.2.0f3.pkg"},
            {new UnityVersion("2017.1.5"), "https://download.unity3d.com/download_unity/9758a36cfaa6/MacEditorInstaller/Unity-2017.1.5f1.pkg"},
            {new UnityVersion("2017.1.4"), "https://download.unity3d.com/download_unity/9fd71167a288/MacEditorInstaller/Unity-2017.1.4f1.pkg"},
            {new UnityVersion("2017.1.3"), "https://download.unity3d.com/download_unity/574eeb502d14/MacEditorInstaller/Unity-2017.1.3f1.pkg"},
            {new UnityVersion("2017.1.2"), "https://download.unity3d.com/download_unity/cc85bf6a8a04/MacEditorInstaller/Unity-2017.1.2f1.pkg"},
            {new UnityVersion("2017.1.1"), "https://download.unity3d.com/download_unity/5d30cf096e79/MacEditorInstaller/Unity-2017.1.1f1.pkg"},
            {new UnityVersion("2017.1.0"), "https://download.unity3d.com/download_unity/472613c02cf7/MacEditorInstaller/Unity-2017.1.0f3.pkg"},
            {new UnityVersion("5.6.7"), "https://download.unity3d.com/download_unity/e80cc3114ac1/UnityDownloadAssistant.dmg"},
            {new UnityVersion("5.6.6"), "http://beta.unity3d.com/download/6bac21139588/UnityDownloadAssistant-5.6.6f2.dmg"},
            {new UnityVersion("5.6.5"), "https://download.unity3d.com/download_unity/2cac56bf7bb6/MacEditorInstaller/Unity-5.6.5f1.pkg"},
            {new UnityVersion("5.6.4"), "https://download.unity3d.com/download_unity/ac7086b8d112/MacEditorInstaller/Unity-5.6.4f1.pkg"},
            {new UnityVersion("5.6.3"), "https://download.unity3d.com/download_unity/d3101c3b8468/MacEditorInstaller/Unity-5.6.3f1.pkg"},
            {new UnityVersion("5.6.2"), "https://download.unity3d.com/download_unity/a2913c821e27/MacEditorInstaller/Unity-5.6.2f1.pkg"},
            {new UnityVersion("5.6.1"), "https://download.unity3d.com/download_unity/2860b30f0b54/MacEditorInstaller/Unity-5.6.1f1.pkg"},
            {new UnityVersion("5.6.0"), "https://download.unity3d.com/download_unity/497a0f351392/MacEditorInstaller/Unity-5.6.0f3.pkg"},
            {new UnityVersion("5.5.6"), "https://download.unity3d.com/download_unity/3fb31a95adee/MacEditorInstaller/Unity-5.5.6f1.pkg"},
            {new UnityVersion("5.5.5"), "https://download.unity3d.com/download_unity/d875e6967482/MacEditorInstaller/Unity-5.5.5f1.pkg"},
            {new UnityVersion("5.5.4"), "https://download.unity3d.com/download_unity/8ffd0efd98b1/MacEditorInstaller/Unity-5.5.4f1.pkg"},
            {new UnityVersion("5.5.3"), "https://download.unity3d.com/download_unity/4d2f809fd6f3/MacEditorInstaller/Unity-5.5.3f1.pkg"},
            {new UnityVersion("5.5.2"), "https://download.unity3d.com/download_unity/3829d7f588f3/MacEditorInstaller/Unity-5.5.2f1.pkg"},
            {new UnityVersion("5.5.1"), "https://download.unity3d.com/download_unity/88d00a7498cd/MacEditorInstaller/Unity-5.5.1f1.pkg"},
            {new UnityVersion("5.5.0"), "https://download.unity3d.com/download_unity/38b4efef76f0/MacEditorInstaller/Unity-5.5.0f3.pkg"},
            {new UnityVersion("5.4.6"), "https://download.unity3d.com/download_unity/7c5210d1343f/MacEditorInstaller/Unity-5.4.6f3.pkg"},
            {new UnityVersion("5.4.5"), "https://download.unity3d.com/download_unity/68943b6c8c42/MacEditorInstaller/Unity-5.4.5f1.pkg"},
            {new UnityVersion("5.4.4"), "https://download.unity3d.com/download_unity/5a3967d8c55d/MacEditorInstaller/Unity-5.4.4f1.pkg"},
            {new UnityVersion("5.4.3"), "https://download.unity3d.com/download_unity/01f4c123905a/MacEditorInstaller/Unity-5.4.3f1.pkg"},
            {new UnityVersion("5.4.2"), "https://download.unity3d.com/download_unity/b7e030c65c9b/MacEditorInstaller/Unity-5.4.2f2.pkg"},
            {new UnityVersion("5.4.1"), "https://download.unity3d.com/download_unity/649f48bbbf0f/MacEditorInstaller/Unity-5.4.1f1.pkg"},
            {new UnityVersion("5.4.0"), "https://download.unity3d.com/download_unity/a6d8d714de6f/MacEditorInstaller/Unity-5.4.0f3.pkg"},
            {new UnityVersion("5.3.8"), "https://download.unity3d.com/download_unity/0c7e33ff9c0e/MacEditorInstaller/Unity-5.3.8f2.pkg"},
            {new UnityVersion("5.3.7"), "https://download.unity3d.com/download_unity/c347874230fb/MacEditorInstaller/Unity-5.3.7f1.pkg"},
            {new UnityVersion("5.3.6"), "https://download.unity3d.com/download_unity/29055738eb78/MacEditorInstaller/Unity-5.3.6f1.pkg"},
            {new UnityVersion("5.3.5"), "https://download.unity3d.com/download_unity/960ebf59018a/MacEditorInstaller/Unity-5.3.5f1.pkg"},
            {new UnityVersion("5.3.4"), "https://download.unity3d.com/download_unity/fdbb5133b820/MacEditorInstaller/Unity-5.3.4f1.pkg"},
            {new UnityVersion("5.3.3"), "https://download.unity3d.com/download_unity/910d71450a97/MacEditorInstaller/Unity-5.3.3f1.pkg"},
            {new UnityVersion("5.3.2"), "https://download.unity3d.com/download_unity/e87ab445ead0/MacEditorInstaller/Unity-5.3.2f1.pkg"},
            {new UnityVersion("5.3.1"), "https://download.unity3d.com/download_unity/cc9cbbcc37b4/MacEditorInstaller/Unity-5.3.1f1.pkg"},
            {new UnityVersion("5.3.0"), "https://download.unity3d.com/download_unity/2524e04062b4/MacEditorInstaller/Unity-5.3.0f4.pkg"},
            {new UnityVersion("5.2.5"), "https://download.unity3d.com/download_unity/ad2d0368e248/MacEditorInstaller/Unity-5.2.5f1.pkg"},
            {new UnityVersion("5.2.4"), "https://download.unity3d.com/download_unity/98095704e6fe/MacEditorInstaller/Unity-5.2.4f1.pkg"},
            {new UnityVersion("5.2.3"), "https://download.unity3d.com/download_unity/f3d16a1fa2dd/MacEditorInstaller/Unity-5.2.3f1.pkg"},
            {new UnityVersion("5.2.2"), "https://download.unity3d.com/download_unity/3757309da7e7/MacEditorInstaller/Unity-5.2.2f1.pkg"},
            {new UnityVersion("5.2.1"), "https://download.unity3d.com/download_unity/44735ea161b3/MacEditorInstaller/Unity-5.2.1f1.pkg"},
            {new UnityVersion("5.2.0"), "https://download.unity3d.com/download_unity/e7947df39b5c/MacEditorInstaller/Unity-5.2.0f3.pkg"},
            {new UnityVersion("5.1.5"), "https://download.unity3d.com/download_unity/9de525f1a6a8/MacEditorInstaller/Unity-5.1.5f1.pkg"},
            {new UnityVersion("5.1.4"), "https://download.unity3d.com/download_unity/36d0f3617432/MacEditorInstaller/Unity-5.1.4f1.pkg"},
            {new UnityVersion("5.1.3"), "https://download.unity3d.com/download_unity/b0a23b31c3d8/MacEditorInstaller/Unity-5.1.3f1.pkg"},
            {new UnityVersion("5.1.2"), "https://download.unity3d.com/download_unity/afd2369b692a/MacEditorInstaller/Unity-5.1.2f1.pkg"},
            {new UnityVersion("5.1.1"), "https://download.unity3d.com/download_unity/2046fc06d4d8/MacEditorInstaller/Unity-5.1.1f1.pkg"},
            {new UnityVersion("5.1.0"), "https://download.unity3d.com/download_unity/ec70b008569d/MacEditorInstaller/Unity-5.1.0f3.pkg"},
            {new UnityVersion("5.0.4"), "https://download.unity3d.com/download_unity/1d75c08f1c9c/MacEditorInstaller/Unity-5.0.4f1.pkg"},
            {new UnityVersion("5.0.3"), "https://download.unity3d.com/download_unity/c28c7860811c/MacEditorInstaller/Unity-5.0.3f2.pkg"},
            {new UnityVersion("5.0.2"), "https://download.unity3d.com/download_unity/0b02744d4013/MacEditorInstaller/Unity-5.0.2f1.pkg"},
            {new UnityVersion("5.0.1"), "https://download.unity3d.com/download_unity/5a2e8fe35a68/MacEditorInstaller/Unity-5.0.1f1.pkg"},
            {new UnityVersion("5.0.0"), "https://download.unity3d.com/download_unity/5b98b70ebeb9/MacEditorInstaller/Unity-5.0.0f4.pkg"},
            {new UnityVersion("4.7.2"), "https://download.unity3d.com/download_unity/unity-4.7.2.dmg"},
            {new UnityVersion("4.7.1"), "https://download.unity3d.com/download_unity/unity-4.7.1.dmg"},
            {new UnityVersion("4.7.0"), "https://beta.unity3d.com/download/3733150244/unity-4.7.0.dmg"},
            {new UnityVersion("4.6.9"), "https://beta.unity3d.com/download/7083239589/unity-4.6.9.dmg"},
            {new UnityVersion("4.6.8"), "https://download.unity3d.com/download_unity/unity-4.6.8.dmg"},
            {new UnityVersion("4.6.7"), "https://download.unity3d.com/download_unity/unity-4.6.7.dmg"},
            {new UnityVersion("4.6.6"), "https://download.unity3d.com/download_unity/unity-4.6.6.dmg"},
            {new UnityVersion("4.6.5"), "https://download.unity3d.com/download_unity/unity-4.6.5.dmg"},
            {new UnityVersion("4.6.4"), "https://download.unity3d.com/download_unity/unity-4.6.4.dmg"},
            {new UnityVersion("4.6.3"), "https://download.unity3d.com/download_unity/unity-4.6.3.dmg"},
            {new UnityVersion("4.6.2"), "https://download.unity3d.com/download_unity/unity-4.6.2.dmg"},
            {new UnityVersion("4.6.1"), "https://download.unity3d.com/download_unity/unity-4.6.1.dmg"},
            {new UnityVersion("4.6.0"), "https://download.unity3d.com/download_unity/unity-4.6.0.dmg"},
            {new UnityVersion("4.5.5"), "https://download.unity3d.com/download_unity/unity-4.5.5.dmg"},
            {new UnityVersion("4.5.4"), "https://download.unity3d.com/download_unity/unity-4.5.4.dmg"},
            {new UnityVersion("4.5.3"), "https://download.unity3d.com/download_unity/unity-4.5.3.dmg"},
            {new UnityVersion("4.5.2"), "https://download.unity3d.com/download_unity/unity-4.5.2.dmg"},
            {new UnityVersion("4.5.1"), "https://download.unity3d.com/download_unity/unity-4.5.1.dmg"},
            {new UnityVersion("4.5.0"), "https://download.unity3d.com/download_unity/unity-4.5.0.dmg"},
            {new UnityVersion("4.3.4"), "https://download.unity3d.com/download_unity/unity-4.3.4.dmg"},
            {new UnityVersion("4.3.3"), "https://download.unity3d.com/download_unity/unity-4.3.3.dmg"},
            {new UnityVersion("4.3.2"), "https://download.unity3d.com/download_unity/unity-4.3.2.dmg"},
            {new UnityVersion("4.3.1"), "https://download.unity3d.com/download_unity/unity-4.3.1.dmg"},
            {new UnityVersion("4.3.0"), "https://download.unity3d.com/download_unity/unity-4.3.0.dmg"},
            {new UnityVersion("4.2.2"), "https://download.unity3d.com/download_unity/unity-4.2.2.dmg"},
            {new UnityVersion("4.2.1"), "https://download.unity3d.com/download_unity/unity-4.2.1.dmg"},
            {new UnityVersion("4.2.0"), "https://download.unity3d.com/download_unity/unity-4.2.0.dmg"},
            {new UnityVersion("4.1.5"), "https://download.unity3d.com/download_unity/unity-4.1.5.dmg"},
            {new UnityVersion("4.1.4"), "https://download.unity3d.com/download_unity/unity-4.1.4.dmg"},
            {new UnityVersion("4.1.3"), "https://download.unity3d.com/download_unity/unity-4.1.3.dmg"},
            {new UnityVersion("4.1.2"), "https://download.unity3d.com/download_unity/unity-4.1.2.dmg"},
            {new UnityVersion("4.1.0"), "https://download.unity3d.com/download_unity/unity-4.1.0.dmg"},
            {new UnityVersion("4.0.1"), "https://download.unity3d.com/download_unity/unity-4.0.1.dmg"},
            {new UnityVersion("4.0.0"), "https://download.unity3d.com/download_unity/unity-4.0.0.dmg"},
            {new UnityVersion("3.5.7"), "https://download.unity3d.com/download_unity/unity-3.5.7.dmg"},
            {new UnityVersion("3.5.6"), "https://download.unity3d.com/download_unity/unity-3.5.6.dmg"},
            {new UnityVersion("3.5.5"), "https://download.unity3d.com/download_unity/unity-3.5.5.dmg"},
            {new UnityVersion("3.5.4"), "https://download.unity3d.com/download_unity/unity-3.5.4.dmg"},
            {new UnityVersion("3.5.3"), "https://download.unity3d.com/download_unity/unity-3.5.3.dmg"},
            {new UnityVersion("3.5.2"), "https://download.unity3d.com/download_unity/unity-3.5.2.dmg"},
            {new UnityVersion("3.5.1"), "https://download.unity3d.com/download_unity/unity-3.5.1.dmg"},
            {new UnityVersion("3.5.0"), "https://download.unity3d.com/download_unity/unity-3.5.0.dmg"},
            {new UnityVersion("3.4.2"), "https://download.unity3d.com/download_unity/unity-3.4.2.dmg"},
            {new UnityVersion("3.4.1"), "https://download.unity3d.com/download_unity/unity-3.4.1.dmg"},
            {new UnityVersion("3.4.0"), "https://download.unity3d.com/download_unity/unity-3.4.0.dmg"}
        };

        private static string UnityDownloadURL = "https://unity3d.com/de/get-unity/download/archive";
        private static NLog.Logger Logger = NLog.LogManager.GetLogger("UnityVersion");
        private string _Version;
        private static Regex VersionRegex;

        static UnityVersion()
        {
        }
        public string Version
        {
            get
            {
                return _Version;
            }
            private set
            {
                if (_Version != value)
                {
                    _Version = value;
                    if (VersionRegex == null)
                        VersionRegex = new Regex(@"([0-9]+)\.?([0-9]+)?\.?([0-9]+)?([fpb][0-9]+)?");

                    var match = VersionRegex.Match(value);
                    var count = match.Groups.Count;
                    switch (count)
                    {
                        case 5:
                            Appendix = match.Groups[4].Value;
                            goto case 4;
                        case 4:
                            Build = int.Parse(match.Groups[3].Value);
                            goto case 3;
                        case 3:
                            Minor = int.Parse(match.Groups[2].Value);
                            goto case 2;
                        case 2:
                            Major = int.Parse(match.Groups[1].Value);
                            break;
                    }
                    this.RaisePropertyChanged<UnityVersion>("Version");
                }

            }
        }

        public bool IsSupported
        {
            get
            {
                return Major >= 1000;
            }
        }



        private int _Major;
        public int Major { get => _Major; set => this.RaiseAndSetIfChanged<UnityVersion, int>(ref _Major, value, "Major"); }
        private int _Minor;
        public int Minor { get => _Minor; set => this.RaiseAndSetIfChanged<UnityVersion, int>(ref _Minor, value, "Minor"); }
        private int _Build;
        public int Build { get => _Build; set => this.RaiseAndSetIfChanged<UnityVersion, int>(ref _Build, value, "Build"); }

        private string _Appendix;
        public string Appendix { get => _Appendix; set => this.RaiseAndSetIfChanged<UnityVersion, string>(ref _Appendix, value, "Appendix"); }

        public UnityVersion HighestSubVersion
        {
            get
            {
                var start = Major + "." + Minor;
                var highest = new UnityVersion(start + ".0");
                foreach (var version in WindowsDownloadLinks)
                {
                    if (version.Key > highest)
                    {
                        highest = version.Key;
                    }
                }
                return highest;
            }
        }

        public static void DownloadVersions()
        {
            var webClient = new WebClient();
            var regex = new Regex("<h4>Unity ([0-9][^<]+)(?s:.)+?(?=Downloads \\(Mac\\))(?s:.)+?(?=href=\"[^\"]+\">Unity Editor)href=\"([^\"]+)(?s:.)+?(?=Downloads \\(Win\\))(?s:.)+?(?=href=\"[^\"]+\">Unity Editor)href=\"([^\"]+)");
            Logger.Info("Downloading Unity versions URLs...");
            try
            {
                var html = webClient.DownloadString(UnityDownloadURL);
                var match = regex.Match(html);
                do
                {
                    var windowsURL = match.Groups[3].Value;
                    var ind = windowsURL.IndexOf("UnitySetup");
                    var ind2 = windowsURL.IndexOf("-", ind);
                    var ind3 = windowsURL.IndexOf(".exe", ind2);
                    if (ind > -1 && ind2 > -1 && ind3 > -1)
                    {
                        var version = new UnityVersion(windowsURL.Substring(ind2 + 1, ind3 - (ind2 + 1)));

                        WindowsDownloadLinks[version] = match.Groups[3].Value;
                        MacDownloadLinks[version] = match.Groups[2].Value;
                    }
                    else Logger.Warn("Couldn't parse unity version from " + windowsURL);
                    match = match.NextMatch();
                }
                while (match != null && match.Success);
            }
            catch (WebException e)
            {
                Logger.Error(e, "Error while retrieving unity versions online. Are you connected to the internet? Using fallback URLs...");
            }
            catch (Exception e2)
            {
                Logger.Error(e2, "Unknown error happened during retrival of unity versions online. Using fallback URLs...");
            }
        }

        public UnityVersion(string version)
        {
            Version = version;
        }

        public UnityVersion(Game game)
        {
            var gameManagersFile = new FileInfo(Path.Combine(Path.GetFullPath(game.DataDirectory), "globalgamemanagers"));
            Logger.Debug("Looking for unity version in file \"" + gameManagersFile.FullName + "\"");

            if (gameManagersFile.Exists)
            {
                using (var stream = gameManagersFile.OpenRead())
                {
                    stream.Position = 0x14;
                    byte[] buffer = new byte[20];
                    stream.Read(buffer, 0, buffer.Length);
                    for (var i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i] == 0x00)
                        {
                            Version = System.Text.Encoding.UTF8.GetString(buffer, 0, i);
                            Logger.Debug("Found version: " + Version);
                            break;
                        }
                    }
                }
                if (Version == null)
                    throw new Exception("Unity version couldn't be found for game " + game.DisplayName);
            }
            else throw new ArgumentException("Unity version couldn't be found for game " + game.DisplayName);
            Logger.Debug("Finished looking for unity version!");
        }

        public override int GetHashCode()
        {
            return Version.GetHashCode();
        }
        public static bool operator <(UnityVersion version1, UnityVersion version2)
        {
            return version1.Major < version2.Major || version1.Minor < version2.Minor || version1.Build < version2.Build;
        }
        public static bool operator >(UnityVersion version1, UnityVersion version2)
        {
            return version1.Major > version2.Major || version1.Minor > version2.Minor || version1.Build > version2.Build;
        }
        public static bool operator <=(UnityVersion version1, UnityVersion version2)
        {
            return version1.Major < version2.Major || (version1.Major == version2.Major && (version1.Minor < version2.Minor || (version1.Minor == version2.Minor && version1.Build <= version2.Build)));
        }
        public static bool operator >=(UnityVersion version1, UnityVersion version2)
        {
            return version1.Major > version2.Major || (version1.Major == version2.Major && (version1.Minor > version2.Minor || (version1.Minor == version2.Minor && version1.Build >= version2.Build)));
        }
        public static bool operator ==(UnityVersion version1, UnityVersion version2)
        {
            return version1.Major == version2.Major && version1.Minor == version2.Minor && version1.Build == version2.Build && version1.Appendix == version2.Appendix;
        }
        public static bool operator !=(UnityVersion version1, UnityVersion version2)
        {
            return version1.Major != version2.Major || version1.Minor != version2.Minor || version1.Build != version2.Build || version1.Appendix != version2.Appendix;
        }


        public override bool Equals(object obj)
        {
            if (obj is UnityVersion v)
            {
                return Version == v.Version;
            }
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return Version;
        }
    }
}
