using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

[InitializeOnLoad]
public class SwitchDll : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    static string pathIL2CPP = "Assets/NuitrackSDK/Nuitrack/NuitrackAssembly/IL2CPP/nuitrack.net.dll";
    static string pathMONO = "Assets/NuitrackSDK/Nuitrack/NuitrackAssembly/nuitrack.net.dll";
    static string pathIOS = "Assets/NuitrackSDK/Nuitrack/NuitrackAssembly/IOS/nuitrack.net.dll";

    static SwitchDll()
    {
        Check();
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        Check();
    }

    //[MenuItem("Nuitrack/Auto switch dll")]
    public static void Check()
    {
        PluginImporter pluginIL2CPP = (PluginImporter)PluginImporter.GetAtPath(pathIL2CPP);
        PluginImporter pluginMONO = (PluginImporter)PluginImporter.GetAtPath(pathMONO);
        PluginImporter pluginIOS = (PluginImporter)PluginImporter.GetAtPath(pathIOS);

        if (pluginIL2CPP == null)
        {
            Debug.LogError("Il2cpp Dll not found: " + pathIL2CPP);
            return;
        }

        if (pluginMONO == null)
        {
            Debug.LogError("Mono Dll not found: " + pathMONO);
            return;
        }

        if (pluginIOS == null)
        {
            Debug.LogError("IOS Dll not found: " + pathIOS);
            return;
        }

        BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
        BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

        ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(buildTargetGroup);

        bool useStructureSensor = false;

        if (buildTargetGroup == BuildTargetGroup.iOS)
        {
#if use_structure_sensor
            useStructureSensor = true;
#else
            Debug.Log("If you need to use Structure Sensor add use_structure_sensor to Scripting Define Symbols in Player Settings...");
#endif
            Debug.Log("Used Structure Sensor: " + useStructureSensor);
        }

        if (buildTargetGroup == BuildTargetGroup.iOS)
        {
            SwitchCompatibleWithPlatform(pluginMONO, false);

            if (useStructureSensor)
            {
                SwitchCompatibleWithPlatform(pluginIL2CPP, false);
                SwitchCompatibleWithPlatform(pluginIOS, true);
            }
            else
            {
                SwitchCompatibleWithPlatform(pluginIL2CPP, true);
                SwitchCompatibleWithPlatform(pluginIOS, false);
            }
        }
        else if((buildTargetGroup == BuildTargetGroup.Android || buildTargetGroup == BuildTargetGroup.Standalone) && backend == ScriptingImplementation.IL2CPP)
        {
            SwitchCompatibleWithPlatform(pluginIL2CPP, true);
            SwitchCompatibleWithPlatform(pluginMONO, false);
            SwitchCompatibleWithPlatform(pluginIOS, false);
        }
        else
        {
            SwitchCompatibleWithPlatform(pluginIL2CPP, false);
            SwitchCompatibleWithPlatform(pluginMONO, true);
            SwitchCompatibleWithPlatform(pluginIOS, false);
        }

        string backendMessage = "Current Scripting Backend " + PlayerSettings.GetScriptingBackend(buildTargetGroup) + "  Target:" + buildTargetGroup;

        try
        {
            nuitrack.Nuitrack.Init();

            string initSuccessMessage = "<color=green><b>Test Nuitrack (ver." + nuitrack.Nuitrack.GetVersion() + ") init was successful!</b></color>\n" + backendMessage;
            if (nuitrack.Nuitrack.GetDeviceList().Count > 0)
            {
                for (int i = 0; i < nuitrack.Nuitrack.GetDeviceList().Count; i++)
                {
                    nuitrack.device.NuitrackDevice device = nuitrack.Nuitrack.GetDeviceList()[i];
                    string sensorName = device.GetInfo(nuitrack.device.DeviceInfoType.DEVICE_NAME);
                    initSuccessMessage += "\nDevice " + i + " [Sensor Name: " + sensorName + ", License: " + device.GetActivationStatus() + "]";
                }
            }
            else
            {
                initSuccessMessage += "\nSensor not connected";
            }

            nuitrack.Nuitrack.Release();
            Debug.Log(initSuccessMessage);
        }
        catch (System.Exception ex)
        {
            if (ex.ToString().Contains("TBB"))
                TBBReplacer.ShowMessage();

            Debug.LogWarning("<color=red><b>Test Nuitrack init failed!</b></color>\n" +
                "<color=red><b>It is recommended to test on allModulesScene</b></color>\n" + backendMessage);
        }
    }

    public static void SwitchCompatibleWithPlatform(PluginImporter plugin, bool value)
    {
        if (value && plugin.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64) != value)
            Debug.Log("Platform " + EditorUserBuildSettings.activeBuildTarget + ". Nuitrack dll switched to " + plugin.assetPath);

        plugin.SetCompatibleWithAnyPlatform(false);
        plugin.SetCompatibleWithPlatform(BuildTarget.iOS, value);
        plugin.SetCompatibleWithPlatform(BuildTarget.Android, value);
        //plugin.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux, value);
        plugin.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, value);
        plugin.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, value);
        plugin.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, value);
        plugin.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, value);
        plugin.SetCompatibleWithEditor(value);
    }
}
