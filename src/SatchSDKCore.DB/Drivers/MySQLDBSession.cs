using MySqlConnector;
using System;
using System.Data.Common;

namespace SSC.DB.Drivers;

/// <summary>
/// MySql DBSession implementation
/// </summary>
internal class MySQLDBSession : DBSession
{
    private             object              m_Lock              = new object();
    private             MySQLDBInstance     m_Instance          = null!;
    private             string              m_ConnectionString  = string.Empty;
    private             string              m_LogIdentifier     = string.Empty;
    private             MySqlConnection?    m_MySqlDBConnection = null;
    private volatile    bool                m_InUse             = false;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override DBInstance DBInstance => m_Instance;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mySqlDBInstance">Parent MySqlDBInstance</param>
    /// <param name="connectionString">Connection string for the driver</param>
    internal MySQLDBSession(MySQLDBInstance mySqlDBInstance, string connectionString, string logIdentifier)
    {
        m_Instance          = mySqlDBInstance;
        m_ConnectionString  = connectionString;
        m_LogIdentifier     = logIdentifier;

        OpenDBConnection();
        /*var ee = new MySqlCommand("insert into oc_accounts(uid, data) VALUES (@arg1, @arg2)", m_MySqlDBConnection);
        ee.Parameters.Add(new MySqlParameter(){ ParameterName = "@uid"});
        ee.Parameters.Add(new MySqlParameter(){ ParameterName = "@data" });
        ee.Prepare();

        var reader = new MySqlCommand("SELECT\r\n    OWNER_OBJECT_TYPE, OWNER_OBJECT_SCHEMA, OWNER_OBJECT_NAME,\r\n    STATEMENT_NAME, SQL_TEXT\r\nFROM performance_schema.`prepared_statements_instances`;", m_MySqlDBConnection).ExecuteReader();
        while (reader.ReadSocket())
        {
            System.Console.WriteLine(reader.GetValue(0));
            System.Console.WriteLine(reader.GetValue(1));
        }*/
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Return this session into the usable pool for later uses
    /// </summary>
    /// <param name="force">Force the dispose</param>
    public override void Dispose(bool force = false)
    {
        m_Instance.ReleaseSession(this);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Create a DbCommand for the driver
    /// </summary>
    /// <returns>New DbCommand</returns>
    public override DbCommand CreateDbCommand()
    {
        return new MySqlCommand(m_MySqlDBConnection, null);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override void Commit()
    {
        throw new NotImplementedException();
    }

    public override void Rollback()
    {
        throw new NotImplementedException();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try to acquire this DBSession
    /// </summary>
    /// <returns>True if acquired</returns>
    internal bool TryAcquire()
    {
        if (m_InUse)
            return false;

        lock (m_Lock)
        {
            if (m_InUse)
                return false;

            try
            {
                switch (m_MySqlDBConnection?.State)
                {
                    case System.Data.ConnectionState.Closed:
                    case System.Data.ConnectionState.Broken:
                        OpenDBConnection();
                        break;

                }
            }
            catch(Exception) { }

            if (m_MySqlDBConnection?.State != System.Data.ConnectionState.Open)
                return false;

            m_InUse = true;
        }

        return true;
    }
    /// <summary>
    /// Release from a TryAcquire
    /// </summary>
    internal void Release()
    {
        if (!m_InUse)
            return;

        while (m_MySqlDBConnection!.State == System.Data.ConnectionState.Fetching
            || m_MySqlDBConnection!.State == System.Data.ConnectionState.Executing)
            System.Threading.Thread.Yield();

        lock (m_Lock)
            m_InUse = false;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Open a DB Connection
    /// </summary>
    /// <param name="connection">Connection to open</param>
    /// <exception cref="Exception"></exception>
    private void OpenDBConnection()
    {
        try
        {
            if (m_MySqlDBConnection != null)
            {
                try { m_MySqlDBConnection.Close(); }
                catch (Exception) { }
            }

            m_MySqlDBConnection = new MySqlConnection(m_ConnectionString);
            m_MySqlDBConnection.Open();

            MySqlCommand l_Query = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", m_MySqlDBConnection);
            l_Query.ExecuteNonQuery();
        }
        catch (MySqlException l_Exception)
        {
            switch (l_Exception.Number)
            {
                case 0:
                    Logging.Log(ELogSeverity.Error, $"[Database][MySQLDBSession.OpenDBConnection<{m_LogIdentifier}>] Can not connect to the database server");
                    Logging.Log(ELogSeverity.Error, l_Exception);

                    throw new Exception($"[Database][MySQLDBSession.OpenDBConnection<{m_LogIdentifier}>] Can not connect to the database server");

                case 1045:
                case 1042:
                    Logging.Log(ELogSeverity.Error, $"[Database][MySQLDBSession.OpenDBConnection<{m_LogIdentifier}>] Authentification failed");
                    Logging.Log(ELogSeverity.Error, l_Exception);

                    throw new Exception($"[Database][MySQLDBSession.OpenDBConnection<{m_LogIdentifier}>] Authentification failed");

            }
        }
    }
}
