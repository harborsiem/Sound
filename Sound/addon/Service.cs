using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Reflection;

namespace SystemX.Addon {
    internal sealed class Service {
        private const String SoundConfig = "SOUND.CONFIG";
        private const String SoundNamespace = "SystemX.Media.Sound";
        private const String separator = ", ";
        private const string SoundPath = "Sound";

        private static readonly String soundAssembly = Assembly.GetExecutingAssembly().FullName;
        //"Sound, Version=4.0.0.0, Culture=neutral, PublicKeyToken=987df3c2e7e93158";

        //internal providers start
        // Providers for midi devices
        private static readonly String[] midiDeviceProvider = {
            SoundNamespace + ".RealTimeSequencerProvider" + separator + soundAssembly,
            SoundNamespace + ".MidiOutDeviceProvider" + separator + soundAssembly,
            SoundNamespace + ".MidiInDeviceProvider" + separator + soundAssembly,
            SoundNamespace + ".SoftProvider" + separator + soundAssembly,
        };

        // Providers for midi sequences
        private static readonly String[] midiFileReader = {
            SoundNamespace + ".StandardMidiFileReader" + separator + soundAssembly,
        };

        // Providers for Midi file writing
        private static readonly String[] midiFileWriter = {
            SoundNamespace + ".StandardMidiFileWriter" + separator + soundAssembly,
        };

        // Providers for Soundbanks
        private static readonly String[] soundbankReader = {
            SoundNamespace + ".SF2SoundbankReader" + separator + soundAssembly,
            SoundNamespace + ".DLSSoundbankReader" + separator + soundAssembly,
            SoundNamespace + ".AudioFileSoundbankReader" + separator + soundAssembly,
            SoundNamespace + ".JARSoundbankReader" + separator + soundAssembly,
        };

        // Providers for audio file reading
        private static readonly String[] audioFileReader = {
            SoundNamespace + ".WaveExtensibleFileReader" + separator + soundAssembly,
            SoundNamespace + ".AuFileReader" + separator + soundAssembly,
            SoundNamespace + ".AiffFileReader" + separator + soundAssembly,
            SoundNamespace + ".WaveFileReader" + separator + soundAssembly,
            SoundNamespace + ".WaveFloatFileReader" + separator + soundAssembly,
            SoundNamespace + ".SoftMidiAudioFileReader" + separator + soundAssembly,
        };

        // Providers for writing audio files
        private static readonly String[] audioFileWriter = {
            SoundNamespace + ".WaveFloatFileWriter" + separator + soundAssembly,
            SoundNamespace + ".AuFileWriter" + separator + soundAssembly,
            SoundNamespace + ".AiffFileWriter" + separator + soundAssembly,
            SoundNamespace + ".WaveFileWriter" + separator + soundAssembly,
        };

        // Providers for FormatConversion
        private static readonly String[] formatConversionProvider = {
            SoundNamespace + ".AudioFloatFormatConverter" + separator + soundAssembly,
            SoundNamespace + ".UlawCodec" + separator + soundAssembly,
            SoundNamespace + ".AlawCodec" + separator + soundAssembly,
            SoundNamespace + ".PCMtoPCMCodec" + separator + soundAssembly,
        };

        // last mixer is default mixer
        private static readonly String[] mixerProvider = { 
            //SoundNamespace + ".SoftMixingMixerProvider" + separator + soundAssembly,
            SoundNamespace + ".PortMixerProvider" + separator + soundAssembly,
            SoundNamespace + ".DirectAudioDeviceProvider" + separator + soundAssembly,
        };

        private static readonly String s_location = Assembly.GetExecutingAssembly().Location;

        private static readonly IDictionary<Type, IList<String>> serviceTypes = InitializeServiceTypes();
        //internal providers end

        private const String RootProviders = "configuration/providers";
        private const String RootDefaultClasses = "configuration/sound";

        private static IDictionary<Type, IList<String>> spiProviders = InitializeSpiProviders();
        private static IDictionary<String, String> defaultClasses = InitializeDefaultClasses();

        private Service() {
        }

        //internal providers start
        private static IDictionary<Type, IList<String>> InitializeServiceTypes() {
            Dictionary<Type, IList<String>> serviceTypes = new Dictionary<Type, IList<String>>();
            serviceTypes.Add(typeof(SystemX.Sound.Midi.MidiDeviceProvider), midiDeviceProvider);
            serviceTypes.Add(typeof(SystemX.Sound.Midi.MidiFileReader), midiFileReader);
            serviceTypes.Add(typeof(SystemX.Sound.Midi.MidiFileWriter), midiFileWriter);
            serviceTypes.Add(typeof(SystemX.Sound.Midi.SoundbankReader), soundbankReader);
            serviceTypes.Add(typeof(SystemX.Sound.Sampled.AudioFileReader), audioFileReader);
            serviceTypes.Add(typeof(SystemX.Sound.Sampled.AudioFileWriter), audioFileWriter);
            serviceTypes.Add(typeof(SystemX.Sound.Sampled.FormatConversionProvider), formatConversionProvider);
            serviceTypes.Add(typeof(SystemX.Sound.Sampled.MixerProvider), mixerProvider);
            return serviceTypes;
        }
        //internal providers end

        /// <summary>
        /// Normally folder C:\Windows\Microsoft.NET
        /// </summary>
        /// <returns>The Home path</returns>
        public static String GetHome() {
            return Path.Combine(new DirectoryInfo(Environment.SystemDirectory).Parent.FullName, "Microsoft.NET");
        }

        public static String GetClassPathDirectory() {
            String path = Path.Combine(GetHome(), SoundPath);
            if (IntPtr.Size == 4) {
                path = Path.Combine(path, "x86");
            } else {
                path = Path.Combine(path, "x64");
            }
            if (!Directory.Exists(path)) {
                path = null;
            }
            return path;
        }

        public static String GetClassPath() {
            try {
                String path = GetClassPathDirectory();
                if (path != null) {
                    String[] strings = Directory.GetFileSystemEntries(path);
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < strings.Length; i++) {
                        builder.Append(strings[i]);
                        builder.Append(Path.PathSeparator);
                    }
                    if (builder.Length > 0) {
                        builder.Length -= 1;
                    }
                    return builder.ToString();
                }
            } catch (DirectoryNotFoundException) {
            }
            return String.Empty;
        }

        public static IDictionary<String, String> GetDefaultClasses() {
            return defaultClasses;
        }

        private static IDictionary<String, String> InitializeDefaultClasses() {
            Dictionary<String, String> defaultClasses = new Dictionary<String, String>();
            String appDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            String soundConfig = Path.Combine(appDir, "sound.config");
            bool soundConfigInAppDir = new FileInfo(soundConfig).Exists ? true : false;
            if (!soundConfigInAppDir) {
                soundConfig = GetSoundConfig(GetSoundConfigFiles());
                if (soundConfig == null) {
                    return DefaultDefaultClasses(defaultClasses);
                }
            }
            GetXmlProps(soundConfig, null, defaultClasses);
            return defaultClasses;
        }

        private static String[] GetSoundConfigFiles() {
            try {
                String path = Path.Combine(GetHome(), SoundPath);
                if (Directory.Exists(path)) {
                    String[] strings = Directory.GetFiles(path, "*.config", SearchOption.TopDirectoryOnly);
                    return strings;
                }
            } catch (DirectoryNotFoundException) {
            }
            return new String[0];
        }

        private static String GetSoundConfig(String[] configFiles) {
            String soundConfigDir = null;
            for (int i = 0; i < configFiles.Length; i++) {
                String configFile = Path.GetFileName(configFiles[i]);
                if (configFile.ToUpperInvariant().Equals(SoundConfig)) {
                    soundConfigDir = configFiles[i];
                    break;
                }
            }
            return soundConfigDir;
        }

        private static IDictionary<String, String> DefaultDefaultClasses(IDictionary<String, String> defaultClasses) {
            defaultClasses.Add("SystemX.Sound.Midi.IReceiver", String.Empty);
            defaultClasses.Add("SystemX.Sound.Midi.ISequencer", String.Empty);
            defaultClasses.Add("SystemX.Sound.Midi.ISynthesizer", String.Empty);
            defaultClasses.Add("SystemX.Sound.Midi.ITransmitter", String.Empty);
            defaultClasses.Add("SystemX.Sound.Sampled.IClip", String.Empty);
            defaultClasses.Add("SystemX.Sound.Sampled.IPort", String.Empty);
            defaultClasses.Add("SystemX.Sound.Sampled.ISourceDataLine", String.Empty);
            defaultClasses.Add("SystemX.Sound.Sampled.ITargetDataLine", String.Empty);
            return defaultClasses;
        }

        private static IDictionary<Type, IList<String>> InitializeSpiProviders() {
            Dictionary<Type, IList<String>> spiProviders = new Dictionary<Type, IList<String>>();
            IDictionary<String, IList<String>> providers = new Dictionary<String, IList<String>>();
            String[] strings = GetSoundConfigFiles();
            for (int i = 0; i < strings.Length; i++) {
                String configFile = Path.GetFileName(strings[i]);
                if (configFile.ToUpperInvariant().Equals(SoundConfig)) {
                    continue;
                }
                GetXmlProps(strings[i], providers, null);
            }
            IEnumerator<KeyValuePair<String, IList<String>>> it = providers.GetEnumerator();
            while (it.MoveNext()) {
                switch (it.Current.Key) {
                    case "MidiDeviceProvider":
                        spiProviders.Add(typeof(SystemX.Sound.Midi.MidiDeviceProvider), it.Current.Value);
                        break;
                    case "MidiFileReader":
                        spiProviders.Add(typeof(SystemX.Sound.Midi.MidiFileReader), it.Current.Value);
                        break;
                    case "MidiFileWriter":
                        spiProviders.Add(typeof(SystemX.Sound.Midi.MidiFileWriter), it.Current.Value);
                        break;
                    case "SoundbankReader":
                        spiProviders.Add(typeof(SystemX.Sound.Midi.SoundbankReader), it.Current.Value);
                        break;
                    case "AudioFileReader":
                        spiProviders.Add(typeof(SystemX.Sound.Sampled.AudioFileReader), it.Current.Value);
                        break;
                    case "AudioFileWriter":
                        spiProviders.Add(typeof(SystemX.Sound.Sampled.AudioFileWriter), it.Current.Value);
                        break;
                    case "FormatConversionProvider":
                        spiProviders.Add(typeof(SystemX.Sound.Sampled.FormatConversionProvider), it.Current.Value);
                        break;
                    case "MixerProvider":
                        spiProviders.Add(typeof(SystemX.Sound.Sampled.MixerProvider), it.Current.Value);
                        break;
                    default: break;
                }
            }
            AddInternalSpiProviders(spiProviders);
            return spiProviders;
        }

        private static void AddInternalSpiProviders(IDictionary<Type, IList<String>> spiProviders) {
            foreach (KeyValuePair<Type, IList<String>> pair in serviceTypes) {
                if (spiProviders.ContainsKey(pair.Key)) {
                    IList<String> spiValues = spiProviders[pair.Key];
                    IList<String> serviceValues = pair.Value;
                    for (int i = 0; i < serviceValues.Count; i++) {
                        spiValues.Add(serviceValues[i]);
                    }
                } else {
                    spiProviders.Add(pair.Key, pair.Value);
                }
            }
        }

        private static void GetXmlProps(String fileName, IDictionary<String, IList<String>> providers,
            IDictionary<String, String> defaultClasses) {

            try {
                FileInfo file = new FileInfo(fileName);
                if (file.Exists) {
                    XmlDocument doc = GetDocument(file);
                    if (doc != null && providers != null) {
                        LoadProviders(doc, providers);
                    }
                    if (doc != null && defaultClasses != null) {
                        LoadDefaultClasses(doc, defaultClasses);
                    }
                }
            } catch (Exception) {
                throw; //MessageBox.Show(e.StackTrace);
            }
        }

        private static XmlDocument GetDocument(FileInfo file) {
            XmlDocument document = new XmlDocument();
            try {
                document.Load(file.FullName);
            } catch (IOException) {
                return null;
                //MessageBox.Show(ioe.StackTrace);
            }
            return document;
        }

        private static void LoadProviders(XmlDocument xDoc, IDictionary<String, IList<String>> props) {
            try {
                // iterate through Region-Nodes
                foreach (XmlNode regionNode in xDoc.SelectNodes(RootProviders)) {

                    // iterate through Nodes in actual Region-Node
                    foreach (XmlNode specificNode in regionNode.ChildNodes) {

                        if (specificNode.Name == "midi" || specificNode.Name == "sampled") {

                            foreach (XmlNode providersNode in specificNode.ChildNodes) {
                                List<String> list = new List<String>();
                                String providers = providersNode.Name;
                                String provider = null;

                                foreach (XmlNode providerNode in providersNode.ChildNodes) {
                                    provider = providerNode.Name;
                                    XmlAttribute nameAtt = providerNode.Attributes["Name"];
                                    if (!nameAtt.Value.StartsWith("#", StringComparison.Ordinal) && providers.StartsWith(provider, StringComparison.Ordinal)) {
                                        String value;
                                        XmlAttribute descriptionAtt = providerNode.Attributes["Description"];
                                        value = descriptionAtt.Value;
                                        //if (soundAssemblyName != null) {
                                        //    if (!value.Contains("PublicKeyToken=")) {
                                        //        value += separator + soundAssemblyName;
                                        //    }
                                        //}
                                        list.Add(value);
                                    }
                                }
                                if (provider == null) {
                                    provider = providers.Substring(0, providers.Length - 1);
                                }
                                if (props.ContainsKey(provider)) {
                                    IList<String> tmp = props[provider];
                                    for (int i = 0; i < list.Count; i++) {
                                        tmp.Add(list[i]);
                                    }
                                } else {
                                    props.Add(provider, list);
                                }
                            }
                        }
                    }
                }
            } catch (Exception) {
                props.Clear();
                throw;
                //MessageBox.Show(e.StackTrace);
            }
        }

        private static void LoadDefaultClasses(XmlDocument xDoc, IDictionary<String, String> props) {
            try {
                // iterate through Region-Nodes
                foreach (XmlNode regionNode in xDoc.SelectNodes(RootDefaultClasses)) {

                    // iterate through Nodes in actual Region-Node
                    foreach (XmlNode specificNode in regionNode.ChildNodes) {

                        if (specificNode.Name == "midi" || specificNode.Name == "sampled") {

                            foreach (XmlNode defaultsNode in specificNode.ChildNodes) {
                                String defaultClass = String.Empty;
                                String defaultsName = defaultsNode.Name;
                                String defaultName = null;

                                foreach (XmlNode defaultNode in defaultsNode.ChildNodes) {
                                    defaultName = defaultNode.Name;
                                    XmlAttribute interfaceAtt = defaultNode.Attributes["Interface"];
                                    if (defaultsName.StartsWith(defaultName, StringComparison.Ordinal)) {
                                        XmlAttribute defaultClassAtt = defaultNode.Attributes["DefaultClass"];
                                        defaultClass = (defaultClassAtt.Value);
                                    }
                                    props.Add(interfaceAtt.Value, defaultClass);
                                }
                            }
                        }
                    }
                }
            } catch (Exception) {
                props.Clear();
                throw;
                //MessageBox.Show(e.StackTrace);
            }
        }

        public static IEnumerator GetProviders(Type serviceClass) {
            ArrayList objectList = new ArrayList();
            IList<String> providers;
            if (spiProviders.TryGetValue(serviceClass, out providers)) {
                for (int i = 0; i < providers.Count; i++) {
                    string[] elements = providers[i].Split(',');
                    string pluginLocation = null;
                    Type type = null;
                    if (elements.Length > 1) {
                        string dllName = (elements[1].Trim() + ".dll").ToUpperInvariant();
                        if (dllName != "SOUND.DLL") {
                            pluginLocation = Path.Combine(s_location, dllName);
                            if (File.Exists(pluginLocation)) {
                                Assembly ass = Assembly.LoadFrom(pluginLocation);
                                if (ass != null)
                                    type = ass.GetType(elements[0].Trim());
                            }
                        }
                    }
                    if (pluginLocation == null) {
                        type = Type.GetType(providers[i], false);
                    }
                    if (type != null) {
                        Object handle = Activator.CreateInstance(type);
                        objectList.Add(handle);
                    }
                }
            }
            return objectList.GetEnumerator();
        }
    }
}
