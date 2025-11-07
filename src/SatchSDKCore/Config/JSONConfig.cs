using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace SSC.Config
{
    /// <summary>
    /// JSON config file
    /// </summary>
    /// <typeparam name="t_Type">Type</typeparam>
    public abstract class JSONConfig
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] t_Type>
        where t_Type : JSONConfig<t_Type>, new()
    {
        private static t_Type? m_Instance = null;
        private static string  m_Name     = typeof(t_Type).Name;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        private string m_DirectoryPath = string.Empty;
        private string m_FilePath      = string.Empty;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        protected JsonSerializerSettings m_JSONSerializerSettings = new();
        protected JObject?               m_RawLoaded              = null;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Singleton
        /// </summary>
        public static t_Type Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new t_Type();

                return m_Instance;
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        public JSONConfig(string relativePath)
        {
            m_DirectoryPath = Path.GetFullPath(relativePath);
            m_FilePath      = Path.Combine(m_DirectoryPath, $"{m_Name}.json");

            m_JSONSerializerSettings                      = new JsonSerializerSettings();
            m_JSONSerializerSettings.DefaultValueHandling = DefaultValueHandling.Include;
            m_JSONSerializerSettings.NullValueHandling    = NullValueHandling.Ignore;

            if (!TryCreateFolder())
                Environment.Exit(-1);

            try
            {
                if (File.Exists(m_FilePath))
                {
                    using (var l_FileStream = new FileStream(m_FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var l_StreamReader = new StreamReader(l_FileStream, Encoding.UTF8))
                        {
                            var l_Content = l_StreamReader.ReadToEnd();

                            m_RawLoaded = JObject.Parse(l_Content);
                            JsonConvert.PopulateObject(l_Content, this, m_JSONSerializerSettings);
                        }
                    }

                    OnInit(false);
                    m_RawLoaded = null;
                }
                else
                {
                    OnInit(true);
                }

                Save();
            }
            catch (Exception l_Exception)
            {
                Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{m_Name}>.ReadImplementation] Failed to read config file in {m_DirectoryPath}");
                Logging.Log(ELogSeverity.Error, l_Exception);

                TryBackupAndReset();
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Dummy method for warmup
        /// </summary>
        public void Warmup()
        {
            ;
        }
        /// <summary>
        /// Reset config to default
        /// </summary>
        public void Reset()
        {
            if (!TryCreateFolder())
                return;

            try
            {
                var l_Default = new t_Type();
                var l_DefaultSerialized = JsonConvert.SerializeObject(l_Default, m_JSONSerializerSettings);
                JsonConvert.PopulateObject(l_DefaultSerialized, this, m_JSONSerializerSettings);

                Save();
            }
            catch (Exception l_Exception)
            {
                Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{m_Name}>.Reset] Failed");
                Logging.Log(ELogSeverity.Error, l_Exception);
            }
        }
        /// <summary>
        /// Save config file
        /// </summary>
        public void Save()
        {
            if (!TryCreateFolder())
                return;

            try
            {
                string l_Data = JsonConvert.SerializeObject(this, Formatting.Indented, m_JSONSerializerSettings);
                using (var l_FileStream = new FileStream(m_FilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (var l_StreamWritter = new StreamWriter(l_FileStream, Encoding.UTF8))
                    {
                        l_StreamWritter.WriteLine(l_Data);
                    }
                }
            }
            catch (Exception l_Exception)
            {
                Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{m_Name}>.WriteFile] Failed to write file {m_FilePath}");
                Logging.Log(ELogSeverity.Error, l_Exception);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// On config init
        /// </summary>
        /// <param name="onFileCreate">On file create?</param>
        protected virtual void OnInit(bool onFileCreate)
        {

        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Try to create the destination folder
        /// </summary>
        /// <returns></returns>
        private bool TryCreateFolder()
        {
            var l_Directory = Path.GetDirectoryName(m_FilePath);

            try
            {
                if (!Directory.Exists(l_Directory))
                    Directory.CreateDirectory(l_Directory!);

                return true;
            }
            catch (Exception l_Exception)
            {
                Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{typeof(t_Type).Name}>.WriteFile] Failed to create directory " + l_Directory!);
                Logging.Log(ELogSeverity.Error, l_Exception);
            }

            return false;
        }
        /// <summary>
        /// Try to backup the existing file and then reset this config
        /// </summary>
        private void TryBackupAndReset()
        {
            if (File.Exists(m_FilePath))
            {
                try
                {
                    File.Move(m_FilePath,
                        Path.Combine(
                            m_DirectoryPath,
                            Path.GetFileNameWithoutExtension(m_FilePath) + ".broken_" + Misc.Time.UnixTimeNowMS() + ".json"
                        )
                    );
                }
                catch (Exception l_Exception)
                {
                    Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{m_Name}>.WriteFile] Failed to backup file {m_FilePath}, trying deletion...");
                    Logging.Log(ELogSeverity.Error, l_Exception);

                    File.Delete(m_FilePath);
                }
            }

            Reset();
            OnInit(true);
        }
    }
}
