using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinSystemCtl.Core;

namespace WinSystemCtl
{
    public class Settings : ViewModelBase
    {
        private static readonly string FILENAME = "settings.json";

        private int _lastWindowPosX;

        [DefaultValue(0)]
        public int LastWindowPosX
        {
            get => _lastWindowPosX;
            set => Set(ref _lastWindowPosX, value);
        }

        private int _lastWindowPosY;

        [DefaultValue(0)]
        public int LastWindowPosY
        {
            get => _lastWindowPosY;
            set => Set(ref _lastWindowPosY, value);
        }

        private int _lastWindowWidth;

        [DefaultValue(1440)]
        public int LastWindowWidth
        {
            get => _lastWindowWidth;
            set => Set(ref _lastWindowWidth, value);
        }

        private int _lastWindowHeight;

        [DefaultValue(960)]
        public int LastWindowHeight
        {
            get => _lastWindowHeight;
            set => Set(ref _lastWindowHeight, value);
        }

        private int _logBufferSize;

        [DefaultValue(65536)]
        public int LogBufferSize
        {
            get => _logBufferSize;
            set => Set(ref _logBufferSize, value);
        }

        private int _cacheOutputSize;

        [DefaultValue(65536)]
        public int CacheOutputSize
        {
            get => _cacheOutputSize;
            set => Set(ref _cacheOutputSize, value);
        }

        private int _toastAutoCloseTime;

        [DefaultValue(2000)]
        public int ToastAutoCloseTime
        {
            get => _toastAutoCloseTime;
            set => Set(ref _toastAutoCloseTime, value);
        }

        private LanguageOptions _language;

        [DefaultValue(LanguageOptions.en_US)]
        public LanguageOptions Language
        {
            get => _language;
            set => Set(ref _language, value);
        }


        private static Settings _default;

        public static Settings Instance { get; set; }

        static Settings()
        {
            // constructs default
            _default = Utils.GetDefaultInstance<Settings>();

            Instance = _default;
        }

        public static void LoadFromFile()
        {
            if (!File.Exists(Path.Join(Environment.CurrentDirectory, FILENAME)))
            {
                return;
            }
            using TextReader reader = new StreamReader(FILENAME, true);
            JsonSerializer serializer = new();
            serializer.DefaultValueHandling = DefaultValueHandling.Populate;

            try { Instance = serializer.Deserialize(reader, typeof(Settings)) as Settings; }
            catch { Instance = _default; }
        }

        public static void Save()
        {
            using var writer = new StreamWriter(FILENAME, false, System.Text.Encoding.UTF8);
            writer.Write(JsonConvert.SerializeObject(Instance, Formatting.Indented));
        }
    }
}
