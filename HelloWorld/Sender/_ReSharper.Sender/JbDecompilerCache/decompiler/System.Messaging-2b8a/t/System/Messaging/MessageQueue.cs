// Type: System.Messaging.MessageQueue
// Assembly: System.Messaging, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Messaging.dll

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.DirectoryServices;
using System.Globalization;
using System.Messaging.Design;
using System.Messaging.Interop;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Transactions;

namespace System.Messaging
{
  [MessagingDescription("MessageQueueDesc")]
  [DefaultEvent("ReceiveCompleted")]
  [TypeConverter(typeof (MessageQueueConverter))]
  [Editor("System.Messaging.Design.QueuePathEditor", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
  [InstallerType(typeof (MessageQueueInstaller))]
  public class MessageQueue : Component, IEnumerable
  {
    public static readonly TimeSpan InfiniteTimeout = TimeSpan.FromMilliseconds((double) uint.MaxValue);
    public static readonly long InfiniteQueueSize = (long) uint.MaxValue;
    internal static readonly Version OSVersion = Environment.OSVersion.Version;
    internal static readonly Version WinXP = new Version(5, 1);
    internal static readonly bool Msmq3OrNewer = MessageQueue.OSVersion >= MessageQueue.WinXP;
    private static readonly string SUFIX_PRIVATE = "\\PRIVATE$";
    private static readonly string SUFIX_JOURNAL = "\\JOURNAL$";
    private static readonly string SUFIX_DEADLETTER = "\\DEADLETTER$";
    private static readonly string SUFIX_DEADXACT = "\\XACTDEADLETTER$";
    private static readonly string PREFIX_LABEL = "LABEL:";
    private static readonly string PREFIX_FORMAT_NAME = "FORMATNAME:";
    private static MessageQueue.CacheTable<string, string> formatNameCache = new MessageQueue.CacheTable<string, string>("formatNameCache", 4, new TimeSpan(0, 0, 100));
    private static MessageQueue.CacheTable<MessageQueue.QueueInfoKeyHolder, MessageQueue.MQCacheableInfo> queueInfoCache = new MessageQueue.CacheTable<MessageQueue.QueueInfoKeyHolder, MessageQueue.MQCacheableInfo>("queue info", 4, new TimeSpan(0, 0, 100));
    private static bool enableConnectionCache = false;
    private static object staticSyncRoot = new object();
    private object syncRoot = new object();
    private DefaultPropertiesToSend defaultProperties;
    private MessagePropertyFilter receiveFilter;
    private QueueAccessMode accessMode;
    private int sharedMode;
    private string formatName;
    private string queuePath;
    private string path;
    private bool enableCache;
    private QueuePropertyVariants properties;
    private IMessageFormatter formatter;
    private static volatile string computerName;
    private MessageQueue.QueuePropertyFilter filter;
    private bool authenticate;
    private short basePriority;
    private DateTime createTime;
    private int encryptionLevel;
    private Guid id;
    private string label;
    private string multicastAddress;
    private DateTime lastModifyTime;
    private long journalSize;
    private long queueSize;
    private Guid queueType;
    private bool useJournaling;
    private MessageQueue.MQCacheableInfo mqInfo;
    private volatile bool attached;
    private bool useThreadPool;
    private AsyncCallback onRequestCompleted;
    private PeekCompletedEventHandler onPeekCompleted;
    private ReceiveCompletedEventHandler onReceiveCompleted;
    private ISynchronizeInvoke synchronizingObject;
    private volatile Hashtable outstandingAsyncRequests;
    private volatile MessageQueue.QueueInfoKeyHolder queueInfoKey;
    private bool administerGranted;
    private bool browseGranted;
    private bool sendGranted;
    private bool receiveGranted;
    private bool peekGranted;

    public QueueAccessMode AccessMode
    {
      [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")] get
      {
        return this.accessMode;
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [MessagingDescription("MQ_Authenticate")]
    public bool Authenticate
    {
      get
      {
        if (!this.PropertyFilter.Authenticate)
        {
          this.Properties.SetUI1(111, (byte) 0);
          this.GenerateQueueProperties();
          this.authenticate = (int) this.Properties.GetUI1(111) != 0;
          this.PropertyFilter.Authenticate = true;
          this.Properties.Remove(111);
        }
        return this.authenticate;
      }
      set
      {
        if (value)
          this.Properties.SetUI1(111, (byte) 1);
        else
          this.Properties.SetUI1(111, (byte) 0);
        this.SaveQueueProperties();
        this.authenticate = value;
        this.PropertyFilter.Authenticate = true;
        this.Properties.Remove(111);
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [MessagingDescription("MQ_BasePriority")]
    public short BasePriority
    {
      get
      {
        if (!this.PropertyFilter.BasePriority)
        {
          this.Properties.SetI2(106, (short) 0);
          this.GenerateQueueProperties();
          this.basePriority = this.properties.GetI2(106);
          this.PropertyFilter.BasePriority = true;
          this.Properties.Remove(106);
        }
        return this.basePriority;
      }
      set
      {
        this.Properties.SetI2(106, value);
        this.SaveQueueProperties();
        this.basePriority = value;
        this.PropertyFilter.BasePriority = true;
        this.Properties.Remove(106);
      }
    }

    [MessagingDescription("MQ_CanRead")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool CanRead
    {
      get
      {
        if (!this.browseGranted)
        {
          new MessageQueuePermission(MessageQueuePermissionAccess.Browse, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
          this.browseGranted = true;
        }
        return this.MQInfo.CanRead;
      }
    }

    [MessagingDescription("MQ_CanWrite")]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool CanWrite
    {
      get
      {
        if (!this.browseGranted)
        {
          new MessageQueuePermission(MessageQueuePermissionAccess.Browse, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
          this.browseGranted = true;
        }
        return this.MQInfo.CanWrite;
      }
    }

    [MessagingDescription("MQ_Category")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Guid Category
    {
      get
      {
        if (!this.PropertyFilter.Category)
        {
          this.Properties.SetNull(102);
          this.GenerateQueueProperties();
          byte[] numArray = new byte[16];
          IntPtr intPtr = this.Properties.GetIntPtr(102);
          if (intPtr != IntPtr.Zero)
          {
            Marshal.Copy(intPtr, numArray, 0, 16);
            SafeNativeMethods.MQFreeMemory(intPtr);
          }
          this.queueType = new Guid(numArray);
          this.PropertyFilter.Category = true;
          this.Properties.Remove(102);
        }
        return this.queueType;
      }
      set
      {
        this.Properties.SetGuid(102, value.ToByteArray());
        this.SaveQueueProperties();
        this.queueType = value;
        this.PropertyFilter.Category = true;
        this.Properties.Remove(102);
      }
    }

    internal static string ComputerName
    {
      get
      {
        if (MessageQueue.computerName == null)
        {
          lock (MessageQueue.staticSyncRoot)
          {
            if (MessageQueue.computerName == null)
            {
              StringBuilder local_0 = new StringBuilder(256);
              SafeNativeMethods.GetComputerName(local_0, new int[1]
              {
                local_0.Capacity
              });
              MessageQueue.computerName = ((object) local_0).ToString();
            }
          }
        }
        return MessageQueue.computerName;
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [MessagingDescription("MQ_CreateTime")]
    public DateTime CreateTime
    {
      get
      {
        if (!this.PropertyFilter.CreateTime)
        {
          DateTime dateTime = new DateTime(1970, 1, 1);
          this.Properties.SetI4(109, 0);
          this.GenerateQueueProperties();
          this.createTime = dateTime.AddSeconds((double) this.properties.GetI4(109)).ToLocalTime();
          this.PropertyFilter.CreateTime = true;
          this.Properties.Remove(109);
        }
        return this.createTime;
      }
    }

    [MessagingDescription("MQ_DefaultPropertiesToSend")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Browsable(false)]
    public DefaultPropertiesToSend DefaultPropertiesToSend
    {
      get
      {
        if (this.defaultProperties == null)
          this.defaultProperties = !this.DesignMode ? new DefaultPropertiesToSend() : new DefaultPropertiesToSend(true);
        return this.defaultProperties;
      }
      [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")] set
      {
        this.defaultProperties = value;
      }
    }

    [Browsable(false)]
    [DefaultValue(false)]
    [MessagingDescription("MQ_DenySharedReceive")]
    public bool DenySharedReceive
    {
      get
      {
        return this.sharedMode == 1;
      }
      set
      {
        if (value && this.sharedMode != 1)
        {
          this.Close();
          this.sharedMode = 1;
        }
        else
        {
          if (value || this.sharedMode != 1)
            return;
          this.Close();
          this.sharedMode = 0;
        }
      }
    }

    [Browsable(false)]
    public static bool EnableConnectionCache
    {
      [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")] get
      {
        return MessageQueue.enableConnectionCache;
      }
      [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")] set
      {
        MessageQueue.enableConnectionCache = value;
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [MessagingDescription("MQ_EncryptionRequired")]
    public EncryptionRequired EncryptionRequired
    {
      get
      {
        if (!this.PropertyFilter.EncryptionLevel)
        {
          this.Properties.SetUI4(112, 0);
          this.GenerateQueueProperties();
          this.encryptionLevel = this.Properties.GetUI4(112);
          this.PropertyFilter.EncryptionLevel = true;
          this.Properties.Remove(112);
        }
        return (EncryptionRequired) this.encryptionLevel;
      }
      set
      {
        if (!ValidationUtility.ValidateEncryptionRequired(value))
          throw new InvalidEnumArgumentException("value", (int) value, typeof (EncryptionRequired));
        this.Properties.SetUI4(112, (int) value);
        this.SaveQueueProperties();
        this.encryptionLevel = this.properties.GetUI4(112);
        this.PropertyFilter.EncryptionLevel = true;
        this.Properties.Remove(112);
      }
    }

    [MessagingDescription("MQ_FormatName")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string FormatName
    {
      get
      {
        if (this.formatName == null)
        {
          if (this.path == null || this.path.Length == 0)
            return string.Empty;
          string key = this.path.ToUpper(CultureInfo.InvariantCulture);
          if (this.enableCache)
            this.formatName = MessageQueue.formatNameCache.Get(key);
          if (this.formatName == null)
          {
            if (this.PropertyFilter.Id)
            {
              StringBuilder formatName = new StringBuilder(124);
              int count = 124;
              int error = SafeNativeMethods.MQInstanceToFormatName(this.id.ToByteArray(), formatName, ref count);
              if (error != 0)
                throw new MessageQueueException(error);
              this.formatName = ((object) formatName).ToString();
              return this.formatName;
            }
            else
            {
              if (key.StartsWith(MessageQueue.PREFIX_FORMAT_NAME))
              {
                if (!string.IsNullOrWhiteSpace(this.queuePath))
                {
                  this.formatName = MessageQueue.ResolveFormatNameFromQueuePath(this.queuePath, true);
                }
                else
                {
                  this.formatName = this.path.Substring(MessageQueue.PREFIX_FORMAT_NAME.Length);
                  int num = this.formatName.IndexOf(':');
                  if (num >= 0)
                  {
                    try
                    {
                      this.formatName = MessageQueue.ResolveFormatNameFromQueuePath(this.formatName.Substring(num + 1), true);
                    }
                    catch (MessageQueueException ex)
                    {
                    }
                  }
                }
              }
              else if (key.StartsWith(MessageQueue.PREFIX_LABEL))
              {
                MessageQueue messageQueue = MessageQueue.ResolveQueueFromLabel(this.path, true);
                this.formatName = messageQueue.FormatName;
                this.queuePath = messageQueue.QueuePath;
              }
              else
              {
                this.queuePath = this.path;
                this.formatName = MessageQueue.ResolveFormatNameFromQueuePath(this.queuePath, true);
              }
              MessageQueue.formatNameCache.Put(key, this.formatName);
            }
          }
        }
        return this.formatName;
      }
    }

    [MessagingDescription("MQ_Formatter")]
    [DefaultValue(null)]
    [Browsable(false)]
    [TypeConverter(typeof (MessageFormatterConverter))]
    public IMessageFormatter Formatter
    {
      get
      {
        if (this.formatter == null && !this.DesignMode)
          this.formatter = (IMessageFormatter) new XmlMessageFormatter();
        return this.formatter;
      }
      [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")] set
      {
        this.formatter = value;
      }
    }

    [MessagingDescription("MQ_GuidId")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Guid Id
    {
      get
      {
        if (!this.PropertyFilter.Id)
        {
          this.Properties.SetNull(101);
          this.GenerateQueueProperties();
          byte[] numArray = new byte[16];
          IntPtr intPtr = this.Properties.GetIntPtr(101);
          if (intPtr != IntPtr.Zero)
          {
            Marshal.Copy(intPtr, numArray, 0, 16);
            SafeNativeMethods.MQFreeMemory(intPtr);
          }
          this.id = new Guid(numArray);
          this.PropertyFilter.Id = true;
          this.Properties.Remove(101);
        }
        return this.id;
      }
    }

    [MessagingDescription("MQ_Label")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Label
    {
      get
      {
        if (!this.PropertyFilter.Label)
        {
          this.Properties.SetNull(108);
          this.GenerateQueueProperties();
          string str = (string) null;
          IntPtr intPtr = this.Properties.GetIntPtr(108);
          if (intPtr != IntPtr.Zero)
          {
            str = Marshal.PtrToStringUni(intPtr);
            SafeNativeMethods.MQFreeMemory(intPtr);
          }
          this.label = str;
          this.PropertyFilter.Label = true;
          this.Properties.Remove(108);
        }
        return this.label;
      }
      set
      {
        if (value == null)
          throw new ArgumentNullException("value");
        this.Properties.SetString(108, Message.StringToBytes(value));
        this.SaveQueueProperties();
        this.label = value;
        this.PropertyFilter.Label = true;
        this.Properties.Remove(108);
      }
    }

    [MessagingDescription("MQ_LastModifyTime")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateTime LastModifyTime
    {
      get
      {
        if (!this.PropertyFilter.LastModifyTime)
        {
          DateTime dateTime = new DateTime(1970, 1, 1);
          this.Properties.SetI4(110, 0);
          this.GenerateQueueProperties();
          this.lastModifyTime = dateTime.AddSeconds((double) this.properties.GetI4(110)).ToLocalTime();
          this.PropertyFilter.LastModifyTime = true;
          this.Properties.Remove(110);
        }
        return this.lastModifyTime;
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [MessagingDescription("MQ_MachineName")]
    [Browsable(false)]
    public string MachineName
    {
      get
      {
        string queuePath = this.QueuePath;
        if (queuePath.Length == 0)
          return queuePath;
        else
          return queuePath.Substring(0, queuePath.IndexOf('\\'));
      }
      set
      {
        if (value == null)
          throw new ArgumentNullException("value");
        if (!SyntaxCheck.CheckMachineName(value))
        {
          throw new ArgumentException(System.Messaging.Res.GetString("InvalidProperty", (object) "MachineName", (object) value));
        }
        else
        {
          StringBuilder stringBuilder = new StringBuilder();
          if ((this.path == null || this.path.Length == 0) && this.formatName == null)
          {
            stringBuilder.Append(value);
            stringBuilder.Append(MessageQueue.SUFIX_JOURNAL);
          }
          else
          {
            stringBuilder.Append(value);
            stringBuilder.Append("\\");
            stringBuilder.Append(this.QueueName);
          }
          this.Path = ((object) stringBuilder).ToString();
        }
      }
    }

    [MessagingDescription("MQ_MaximumJournalSize")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [TypeConverter(typeof (SizeConverter))]
    public long MaximumJournalSize
    {
      get
      {
        if (!this.PropertyFilter.MaximumJournalSize)
        {
          this.Properties.SetUI4(107, 0);
          this.GenerateQueueProperties();
          this.journalSize = (long) (uint) this.properties.GetUI4(107);
          this.PropertyFilter.MaximumJournalSize = true;
          this.Properties.Remove(107);
        }
        return this.journalSize;
      }
      set
      {
        if (value > MessageQueue.InfiniteQueueSize || value < 0L)
        {
          throw new ArgumentException(System.Messaging.Res.GetString("InvalidProperty", (object) "MaximumJournalSize", (object) value));
        }
        else
        {
          this.Properties.SetUI4(107, (int) (uint) value);
          this.SaveQueueProperties();
          this.journalSize = value;
          this.PropertyFilter.MaximumJournalSize = true;
          this.Properties.Remove(107);
        }
      }
    }

    [MessagingDescription("MQ_MaximumQueueSize")]
    [TypeConverter(typeof (SizeConverter))]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public long MaximumQueueSize
    {
      get
      {
        if (!this.PropertyFilter.MaximumQueueSize)
        {
          this.Properties.SetUI4(105, 0);
          this.GenerateQueueProperties();
          this.queueSize = (long) (uint) this.properties.GetUI4(105);
          this.PropertyFilter.MaximumQueueSize = true;
          this.Properties.Remove(105);
        }
        return this.queueSize;
      }
      set
      {
        if (value > MessageQueue.InfiniteQueueSize || value < 0L)
        {
          throw new ArgumentException(System.Messaging.Res.GetString("InvalidProperty", (object) "MaximumQueueSize", (object) value));
        }
        else
        {
          this.Properties.SetUI4(105, (int) (uint) value);
          this.SaveQueueProperties();
          this.queueSize = value;
          this.PropertyFilter.MaximumQueueSize = true;
          this.Properties.Remove(105);
        }
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [MessagingDescription("MQ_MessageReadPropertyFilter")]
    [Browsable(false)]
    public MessagePropertyFilter MessageReadPropertyFilter
    {
      get
      {
        if (this.receiveFilter == null)
        {
          this.receiveFilter = new MessagePropertyFilter();
          this.receiveFilter.SetDefaults();
        }
        return this.receiveFilter;
      }
      set
      {
        if (value == null)
          throw new ArgumentNullException("value");
        this.receiveFilter = value;
      }
    }

    internal MessageQueue.MQCacheableInfo MQInfo
    {
      get
      {
        if (this.mqInfo == null)
        {
          MessageQueue.MQCacheableInfo mqCacheableInfo = MessageQueue.queueInfoCache.Get(this.QueueInfoKey);
          if (this.sharedMode == 1 || !this.enableCache)
          {
            if (mqCacheableInfo != null)
              mqCacheableInfo.CloseIfNotReferenced();
            this.mqInfo = new MessageQueue.MQCacheableInfo(this.FormatName, this.accessMode, this.sharedMode);
            this.mqInfo.AddRef();
          }
          else if (mqCacheableInfo != null)
          {
            mqCacheableInfo.AddRef();
            this.mqInfo = mqCacheableInfo;
          }
          else
          {
            this.mqInfo = new MessageQueue.MQCacheableInfo(this.FormatName, this.accessMode, this.sharedMode);
            this.mqInfo.AddRef();
            MessageQueue.queueInfoCache.Put(this.QueueInfoKey, this.mqInfo);
          }
        }
        return this.mqInfo;
      }
    }

    [MessagingDescription("MQ_MulticastAddress")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [DefaultValue("")]
    public string MulticastAddress
    {
      get
      {
        if (!MessageQueue.Msmq3OrNewer)
        {
          if (this.DesignMode)
            return string.Empty;
          else
            throw new PlatformNotSupportedException(System.Messaging.Res.GetString("PlatformNotSupported"));
        }
        else
        {
          if (!this.PropertyFilter.MulticastAddress)
          {
            this.Properties.SetNull(125);
            this.GenerateQueueProperties();
            string str = (string) null;
            IntPtr intPtr = this.Properties.GetIntPtr(125);
            if (intPtr != IntPtr.Zero)
            {
              str = Marshal.PtrToStringUni(intPtr);
              SafeNativeMethods.MQFreeMemory(intPtr);
            }
            this.multicastAddress = str;
            this.PropertyFilter.MulticastAddress = true;
            this.Properties.Remove(125);
          }
          return this.multicastAddress;
        }
      }
      set
      {
        if (value == null)
          throw new ArgumentNullException("value");
        if (!MessageQueue.Msmq3OrNewer)
          throw new PlatformNotSupportedException(System.Messaging.Res.GetString("PlatformNotSupported"));
        if (value.Length == 0)
          this.Properties.SetEmpty(125);
        else
          this.Properties.SetString(125, Message.StringToBytes(value));
        this.SaveQueueProperties();
        this.multicastAddress = value;
        this.PropertyFilter.MulticastAddress = true;
        this.Properties.Remove(125);
      }
    }

    [DefaultValue("")]
    [Editor("System.Messaging.Design.QueuePathEditor", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [SettingsBindable(true)]
    [RefreshProperties(RefreshProperties.All)]
    [Browsable(false)]
    [TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [MessagingDescription("MQ_Path")]
    public string Path
    {
      [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")] get
      {
        return this.path;
      }
      set
      {
        if (value == null)
          value = string.Empty;
        if (!MessageQueue.ValidatePath(value, false))
          throw new ArgumentException(System.Messaging.Res.GetString("PathSyntax"));
        if (!string.IsNullOrEmpty(this.path))
          this.Close();
        this.path = value;
      }
    }

    QueuePropertyVariants Properties
    {
      private get
      {
        if (this.properties == null)
          this.properties = new QueuePropertyVariants();
        return this.properties;
      }
    }

    MessageQueue.QueuePropertyFilter PropertyFilter
    {
      private get
      {
        if (this.filter == null)
          this.filter = new MessageQueue.QueuePropertyFilter();
        return this.filter;
      }
    }

    [Browsable(false)]
    [MessagingDescription("MQ_QueueName")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string QueueName
    {
      get
      {
        string queuePath = this.QueuePath;
        if (queuePath.Length == 0)
          return queuePath;
        else
          return queuePath.Substring(queuePath.IndexOf('\\') + 1);
      }
      set
      {
        if (value == null)
          throw new ArgumentNullException("value");
        StringBuilder stringBuilder = new StringBuilder();
        if ((this.path == null || this.path.Length == 0) && this.formatName == null)
        {
          stringBuilder.Append(".\\");
          stringBuilder.Append(value);
        }
        else
        {
          stringBuilder.Append(this.MachineName);
          stringBuilder.Append("\\");
          stringBuilder.Append(value);
        }
        this.Path = ((object) stringBuilder).ToString();
      }
    }

    internal string QueuePath
    {
      get
      {
        if (this.queuePath == null)
        {
          if (this.path == null || this.path.Length == 0)
            return string.Empty;
          string str1 = this.path.ToUpper(CultureInfo.InvariantCulture);
          if (str1.StartsWith(MessageQueue.PREFIX_LABEL))
          {
            MessageQueue messageQueue = MessageQueue.ResolveQueueFromLabel(this.path, true);
            this.formatName = messageQueue.FormatName;
            this.queuePath = messageQueue.QueuePath;
          }
          else if (str1.StartsWith(MessageQueue.PREFIX_FORMAT_NAME))
          {
            this.Properties.SetNull(103);
            this.GenerateQueueProperties();
            string str2 = (string) null;
            IntPtr intPtr = this.Properties.GetIntPtr(103);
            if (intPtr != IntPtr.Zero)
            {
              str2 = Marshal.PtrToStringUni(intPtr);
              SafeNativeMethods.MQFreeMemory(intPtr);
            }
            this.Properties.Remove(103);
            this.queuePath = str2;
          }
          else
            this.queuePath = this.path;
        }
        return this.queuePath;
      }
    }

    [Browsable(false)]
    [MessagingDescription("MQ_ReadHandle")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IntPtr ReadHandle
    {
      get
      {
        if (!this.receiveGranted)
        {
          new MessageQueuePermission((MessageQueuePermissionAccess) 26, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
          this.receiveGranted = true;
        }
        return this.MQInfo.ReadHandle.DangerousGetHandle();
      }
    }

    [MessagingDescription("MQ_SynchronizingObject")]
    [Browsable(false)]
    [DefaultValue(null)]
    public ISynchronizeInvoke SynchronizingObject
    {
      get
      {
        if (this.synchronizingObject == null && this.DesignMode)
        {
          IDesignerHost designerHost = (IDesignerHost) this.GetService(typeof (IDesignerHost));
          if (designerHost != null)
          {
            object obj = (object) designerHost.RootComponent;
            if (obj != null && obj is ISynchronizeInvoke)
              this.synchronizingObject = (ISynchronizeInvoke) obj;
          }
        }
        return this.synchronizingObject;
      }
      [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")] set
      {
        this.synchronizingObject = value;
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [MessagingDescription("MQ_Transactional")]
    public bool Transactional
    {
      get
      {
        if (!this.browseGranted)
        {
          new MessageQueuePermission(MessageQueuePermissionAccess.Browse, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
          this.browseGranted = true;
        }
        return this.MQInfo.Transactional;
      }
    }

    [MessagingDescription("MQ_UseJournalQueue")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool UseJournalQueue
    {
      get
      {
        if (!this.PropertyFilter.UseJournalQueue)
        {
          this.Properties.SetUI1(104, (byte) 0);
          this.GenerateQueueProperties();
          this.useJournaling = (int) this.Properties.GetUI1(104) != 0;
          this.PropertyFilter.UseJournalQueue = true;
          this.Properties.Remove(104);
        }
        return this.useJournaling;
      }
      set
      {
        if (value)
          this.Properties.SetUI1(104, (byte) 1);
        else
          this.Properties.SetUI1(104, (byte) 0);
        this.SaveQueueProperties();
        this.useJournaling = value;
        this.PropertyFilter.UseJournalQueue = true;
        this.Properties.Remove(104);
      }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [MessagingDescription("MQ_WriteHandle")]
    public IntPtr WriteHandle
    {
      get
      {
        if (!this.sendGranted)
        {
          new MessageQueuePermission((MessageQueuePermissionAccess) 6, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
          this.sendGranted = true;
        }
        return this.MQInfo.WriteHandle.DangerousGetHandle();
      }
    }

    Hashtable OutstandingAsyncRequests
    {
      private get
      {
        if (this.outstandingAsyncRequests == null)
        {
          lock (this.syncRoot)
          {
            if (this.outstandingAsyncRequests == null)
            {
              Hashtable local_0 = Hashtable.Synchronized(new Hashtable());
              Thread.MemoryBarrier();
              this.outstandingAsyncRequests = local_0;
            }
          }
        }
        return this.outstandingAsyncRequests;
      }
    }

    MessageQueue.QueueInfoKeyHolder QueueInfoKey
    {
      private get
      {
        if (this.queueInfoKey == null)
        {
          lock (this.syncRoot)
          {
            if (this.queueInfoKey == null)
            {
              MessageQueue.QueueInfoKeyHolder local_0 = new MessageQueue.QueueInfoKeyHolder(this.FormatName, this.accessMode);
              Thread.MemoryBarrier();
              this.queueInfoKey = local_0;
            }
          }
        }
        return this.queueInfoKey;
      }
    }

    [MessagingDescription("MQ_PeekCompleted")]
    public event PeekCompletedEventHandler PeekCompleted
    {
      add
      {
        if (!this.peekGranted)
        {
          new MessageQueuePermission((MessageQueuePermissionAccess) 10, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
          this.peekGranted = true;
        }
        this.onPeekCompleted += value;
      }
      remove
      {
        this.onPeekCompleted -= value;
      }
    }

    [MessagingDescription("MQ_ReceiveCompleted")]
    public event ReceiveCompletedEventHandler ReceiveCompleted
    {
      add
      {
        if (!this.receiveGranted)
        {
          new MessageQueuePermission((MessageQueuePermissionAccess) 26, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
          this.receiveGranted = true;
        }
        this.onReceiveCompleted += value;
      }
      remove
      {
        this.onReceiveCompleted -= value;
      }
    }

    static MessageQueue()
    {
    }

    public MessageQueue()
    {
      this.path = string.Empty;
      this.accessMode = QueueAccessMode.SendAndReceive;
    }

    public MessageQueue(string path)
      : this(path, false, MessageQueue.enableConnectionCache)
    {
    }

    public MessageQueue(string path, QueueAccessMode accessMode)
      : this(path, false, MessageQueue.enableConnectionCache, accessMode)
    {
    }

    public MessageQueue(string path, bool sharedModeDenyReceive)
      : this(path, sharedModeDenyReceive, MessageQueue.enableConnectionCache)
    {
    }

    public MessageQueue(string path, bool sharedModeDenyReceive, bool enableCache)
    {
      this.path = path;
      this.enableCache = enableCache;
      if (sharedModeDenyReceive)
        this.sharedMode = 1;
      this.accessMode = QueueAccessMode.SendAndReceive;
    }

    public MessageQueue(string path, bool sharedModeDenyReceive, bool enableCache, QueueAccessMode accessMode)
    {
      this.path = path;
      this.enableCache = enableCache;
      if (sharedModeDenyReceive)
        this.sharedMode = 1;
      this.SetAccessMode(accessMode);
    }

    internal MessageQueue(string path, Guid id)
    {
      this.PropertyFilter.Id = true;
      this.id = id;
      this.path = path;
      this.accessMode = QueueAccessMode.SendAndReceive;
    }

    public IAsyncResult BeginPeek()
    {
      return this.ReceiveAsync(MessageQueue.InfiniteTimeout, CursorHandle.NullHandle, int.MaxValue, (AsyncCallback) null, (object) null);
    }

    public IAsyncResult BeginPeek(TimeSpan timeout)
    {
      return this.ReceiveAsync(timeout, CursorHandle.NullHandle, int.MaxValue, (AsyncCallback) null, (object) null);
    }

    public IAsyncResult BeginPeek(TimeSpan timeout, object stateObject)
    {
      return this.ReceiveAsync(timeout, CursorHandle.NullHandle, int.MaxValue, (AsyncCallback) null, stateObject);
    }

    public IAsyncResult BeginPeek(TimeSpan timeout, object stateObject, AsyncCallback callback)
    {
      return this.ReceiveAsync(timeout, CursorHandle.NullHandle, int.MaxValue, callback, stateObject);
    }

    public IAsyncResult BeginPeek(TimeSpan timeout, Cursor cursor, PeekAction action, object state, AsyncCallback callback)
    {
      if (action != PeekAction.Current && action != PeekAction.Next)
        throw new ArgumentOutOfRangeException(System.Messaging.Res.GetString("InvalidParameter", (object) "action", (object) ((object) action).ToString()));
      else if (cursor == null)
        throw new ArgumentNullException("cursor");
      else
        return this.ReceiveAsync(timeout, cursor.Handle, (int) action, callback, state);
    }

    public IAsyncResult BeginReceive()
    {
      return this.ReceiveAsync(MessageQueue.InfiniteTimeout, CursorHandle.NullHandle, 0, (AsyncCallback) null, (object) null);
    }

    public IAsyncResult BeginReceive(TimeSpan timeout)
    {
      return this.ReceiveAsync(timeout, CursorHandle.NullHandle, 0, (AsyncCallback) null, (object) null);
    }

    public IAsyncResult BeginReceive(TimeSpan timeout, object stateObject)
    {
      return this.ReceiveAsync(timeout, CursorHandle.NullHandle, 0, (AsyncCallback) null, stateObject);
    }

    public IAsyncResult BeginReceive(TimeSpan timeout, object stateObject, AsyncCallback callback)
    {
      return this.ReceiveAsync(timeout, CursorHandle.NullHandle, 0, callback, stateObject);
    }

    public IAsyncResult BeginReceive(TimeSpan timeout, Cursor cursor, object state, AsyncCallback callback)
    {
      if (cursor == null)
        throw new ArgumentNullException("cursor");
      else
        return this.ReceiveAsync(timeout, cursor.Handle, 0, callback, state);
    }

    public static void ClearConnectionCache()
    {
      MessageQueue.formatNameCache.ClearStale(new TimeSpan(0L));
      MessageQueue.queueInfoCache.ClearStale(new TimeSpan(0L));
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public void Close()
    {
      this.Cleanup(true);
    }

    private void Cleanup(bool disposing)
    {
      this.formatName = (string) null;
      this.queuePath = (string) null;
      this.attached = false;
      this.administerGranted = false;
      this.browseGranted = false;
      this.sendGranted = false;
      this.receiveGranted = false;
      this.peekGranted = false;
      if (!disposing || this.mqInfo == null)
        return;
      this.mqInfo.Release();
      if (this.sharedMode == 1 || !this.enableCache)
        this.mqInfo.Dispose();
      this.mqInfo = (MessageQueue.MQCacheableInfo) null;
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public static MessageQueue Create(string path)
    {
      return MessageQueue.Create(path, false);
    }

    public static MessageQueue Create(string path, bool transactional)
    {
      if (path == null)
        throw new ArgumentNullException("path");
      if (path.Length == 0)
        throw new ArgumentException(System.Messaging.Res.GetString("InvalidParameter", (object) "path", (object) path));
      else if (!MessageQueue.IsCanonicalPath(path, true))
      {
        throw new ArgumentException(System.Messaging.Res.GetString("InvalidQueuePathToCreate", new object[1]
        {
          (object) path
        }));
      }
      else
      {
        new MessageQueuePermission((MessageQueuePermissionAccess) 62, "*").Demand();
        QueuePropertyVariants propertyVariants = new QueuePropertyVariants();
        propertyVariants.SetString(103, Message.StringToBytes(path));
        if (transactional)
          propertyVariants.SetUI1(113, (byte) 1);
        else
          propertyVariants.SetUI1(113, (byte) 0);
        StringBuilder formatName = new StringBuilder(124);
        int formatNameLength = 124;
        int queue = System.Messaging.Interop.UnsafeNativeMethods.MQCreateQueue(IntPtr.Zero, propertyVariants.Lock(), formatName, ref formatNameLength);
        propertyVariants.Unlock();
        if (MessageQueue.IsFatalError(queue))
          throw new MessageQueueException(queue);
        else
          return new MessageQueue(path);
      }
    }

    public Cursor CreateCursor()
    {
      return new Cursor(this);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    private static MessageQueue[] CreateMessageQueuesSnapshot(MessageQueueCriteria criteria)
    {
      return MessageQueue.CreateMessageQueuesSnapshot(criteria, true);
    }

    private static MessageQueue[] CreateMessageQueuesSnapshot(MessageQueueCriteria criteria, bool checkSecurity)
    {
      ArrayList arrayList = new ArrayList();
      IEnumerator enumerator = (IEnumerator) MessageQueue.GetMessageQueueEnumerator(criteria, checkSecurity);
      while (enumerator.MoveNext())
      {
        MessageQueue messageQueue = (MessageQueue) enumerator.Current;
        arrayList.Add((object) messageQueue);
      }
      MessageQueue[] messageQueueArray = new MessageQueue[arrayList.Count];
      arrayList.CopyTo((Array) messageQueueArray, 0);
      return messageQueueArray;
    }

    public static void Delete(string path)
    {
      if (path == null)
        throw new ArgumentNullException("path");
      if (path.Length == 0)
      {
        throw new ArgumentException(System.Messaging.Res.GetString("InvalidParameter", (object) "path", (object) path));
      }
      else
      {
        if (!MessageQueue.ValidatePath(path, false))
          throw new ArgumentException(System.Messaging.Res.GetString("PathSyntax"));
        MessageQueue messageQueue = new MessageQueue(path);
        new MessageQueuePermission((MessageQueuePermissionAccess) 62, MessageQueue.PREFIX_FORMAT_NAME + messageQueue.FormatName).Demand();
        int error = System.Messaging.Interop.UnsafeNativeMethods.MQDeleteQueue(messageQueue.FormatName);
        if (MessageQueue.IsFatalError(error))
          throw new MessageQueueException(error);
        MessageQueue.queueInfoCache.Remove(messageQueue.QueueInfoKey);
        MessageQueue.formatNameCache.Remove(path.ToUpper(CultureInfo.InvariantCulture));
      }
    }

    [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
    protected override void Dispose(bool disposing)
    {
      this.Cleanup(disposing);
      base.Dispose(disposing);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public Message EndPeek(IAsyncResult asyncResult)
    {
      return this.EndAsyncOperation(asyncResult);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public Message EndReceive(IAsyncResult asyncResult)
    {
      return this.EndAsyncOperation(asyncResult);
    }

    private Message EndAsyncOperation(IAsyncResult asyncResult)
    {
      if (asyncResult == null)
        throw new ArgumentNullException("asyncResult");
      if (!(asyncResult is MessageQueue.AsynchronousRequest))
        throw new ArgumentException(System.Messaging.Res.GetString("AsyncResultInvalid"));
      else
        return ((MessageQueue.AsynchronousRequest) asyncResult).End();
    }

    public static bool Exists(string path)
    {
      if (path == null)
        throw new ArgumentNullException("path");
      if (!MessageQueue.ValidatePath(path, false))
        throw new ArgumentException(System.Messaging.Res.GetString("PathSyntax"));
      new MessageQueuePermission(MessageQueuePermissionAccess.Browse, "*").Demand();
      string str = path.ToUpper(CultureInfo.InvariantCulture);
      if (str.StartsWith(MessageQueue.PREFIX_FORMAT_NAME))
        throw new InvalidOperationException(System.Messaging.Res.GetString("QueueExistsError"));
      if (str.StartsWith(MessageQueue.PREFIX_LABEL))
      {
        if (MessageQueue.ResolveQueueFromLabel(path, false) == null)
          return false;
        else
          return true;
      }
      else if (MessageQueue.ResolveFormatNameFromQueuePath(path, false) == null)
        return false;
      else
        return true;
    }

    public Message[] GetAllMessages()
    {
      ArrayList arrayList = new ArrayList();
      MessageEnumerator messageEnumerator2 = this.GetMessageEnumerator2();
      while (messageEnumerator2.MoveNext())
      {
        Message current = messageEnumerator2.Current;
        arrayList.Add((object) current);
      }
      Message[] messageArray = new Message[arrayList.Count];
      arrayList.CopyTo((Array) messageArray, 0);
      return messageArray;
    }

    [Obsolete("This method returns a MessageEnumerator that implements RemoveCurrent family of methods incorrectly. Please use GetMessageEnumerator2 instead.")]
    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public IEnumerator GetEnumerator()
    {
      return (IEnumerator) this.GetMessageEnumerator();
    }

    public static Guid GetMachineId(string machineName)
    {
      if (!SyntaxCheck.CheckMachineName(machineName))
      {
        throw new ArgumentException(System.Messaging.Res.GetString("InvalidParameter", (object) "MachineName", (object) machineName));
      }
      else
      {
        if (machineName == ".")
          machineName = MessageQueue.ComputerName;
        new MessageQueuePermission(MessageQueuePermissionAccess.Browse, "*").Demand();
        MachinePropertyVariants propertyVariants = new MachinePropertyVariants();
        byte[] numArray = new byte[16];
        propertyVariants.SetNull(202);
        int machineProperties = System.Messaging.Interop.UnsafeNativeMethods.MQGetMachineProperties(machineName, IntPtr.Zero, propertyVariants.Lock());
        propertyVariants.Unlock();
        IntPtr intPtr = propertyVariants.GetIntPtr(202);
        if (MessageQueue.IsFatalError(machineProperties))
        {
          if (intPtr != IntPtr.Zero)
            SafeNativeMethods.MQFreeMemory(intPtr);
          throw new MessageQueueException(machineProperties);
        }
        else
        {
          if (intPtr != IntPtr.Zero)
          {
            Marshal.Copy(intPtr, numArray, 0, 16);
            SafeNativeMethods.MQFreeMemory(intPtr);
          }
          return new Guid(numArray);
        }
      }
    }

    public static System.Messaging.SecurityContext GetSecurityContext()
    {
      SecurityContextHandle securityContext;
      int securityContextEx = System.Messaging.Interop.NativeMethods.MQGetSecurityContextEx(out securityContext);
      if (MessageQueue.IsFatalError(securityContextEx))
        throw new MessageQueueException(securityContextEx);
      else
        return new System.Messaging.SecurityContext(securityContext);
    }

    public static MessageQueueEnumerator GetMessageQueueEnumerator()
    {
      return new MessageQueueEnumerator((MessageQueueCriteria) null);
    }

    public static MessageQueueEnumerator GetMessageQueueEnumerator(MessageQueueCriteria criteria)
    {
      return new MessageQueueEnumerator(criteria);
    }

    [Obsolete("This method returns a MessageEnumerator that implements RemoveCurrent family of methods incorrectly. Please use GetMessageEnumerator2 instead.")]
    public MessageEnumerator GetMessageEnumerator()
    {
      if (!this.peekGranted)
      {
        new MessageQueuePermission((MessageQueuePermissionAccess) 10, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
        this.peekGranted = true;
      }
      return new MessageEnumerator(this, false);
    }

    public MessageEnumerator GetMessageEnumerator2()
    {
      if (!this.peekGranted)
      {
        new MessageQueuePermission((MessageQueuePermissionAccess) 10, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
        this.peekGranted = true;
      }
      return new MessageEnumerator(this, true);
    }

    public static MessageQueue[] GetPrivateQueuesByMachine(string machineName)
    {
      if (!SyntaxCheck.CheckMachineName(machineName))
      {
        throw new ArgumentException(System.Messaging.Res.GetString("InvalidParameter", (object) "MachineName", (object) machineName));
      }
      else
      {
        new MessageQueuePermission(MessageQueuePermissionAccess.Browse, "*").Demand();
        if (machineName == "." || string.Compare(machineName, MessageQueue.ComputerName, true, CultureInfo.InvariantCulture) == 0)
          machineName = (string) null;
        MessagePropertyVariants propertyVariants = new MessagePropertyVariants(5, 0);
        propertyVariants.SetNull(2);
        int info = System.Messaging.Interop.UnsafeNativeMethods.MQMgmtGetInfo(machineName, "MACHINE", propertyVariants.Lock());
        propertyVariants.Unlock();
        if (MessageQueue.IsFatalError(info))
          throw new MessageQueueException(info);
        uint stringVectorLength = propertyVariants.GetStringVectorLength(2);
        IntPtr vectorBasePointer = propertyVariants.GetStringVectorBasePointer(2);
        MessageQueue[] messageQueueArray = new MessageQueue[(IntPtr) stringVectorLength];
        for (int index = 0; (long) index < (long) stringVectorLength; ++index)
        {
          IntPtr num = Marshal.ReadIntPtr((IntPtr) ((long) vectorBasePointer + (long) (index * IntPtr.Size)));
          string str = Marshal.PtrToStringUni(num);
          messageQueueArray[index] = new MessageQueue("FormatName:DIRECT=OS:" + str);
          messageQueueArray[index].queuePath = str;
          SafeNativeMethods.MQFreeMemory(num);
        }
        SafeNativeMethods.MQFreeMemory(vectorBasePointer);
        return messageQueueArray;
      }
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public static MessageQueue[] GetPublicQueues()
    {
      return MessageQueue.CreateMessageQueuesSnapshot((MessageQueueCriteria) null);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public static MessageQueue[] GetPublicQueues(MessageQueueCriteria criteria)
    {
      return MessageQueue.CreateMessageQueuesSnapshot(criteria);
    }

    public static MessageQueue[] GetPublicQueuesByCategory(Guid category)
    {
      return MessageQueue.CreateMessageQueuesSnapshot(new MessageQueueCriteria()
      {
        Category = category
      });
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public static MessageQueue[] GetPublicQueuesByLabel(string label)
    {
      return MessageQueue.GetPublicQueuesByLabel(label, true);
    }

    private static MessageQueue[] GetPublicQueuesByLabel(string label, bool checkSecurity)
    {
      return MessageQueue.CreateMessageQueuesSnapshot(new MessageQueueCriteria()
      {
        Label = label
      }, checkSecurity);
    }

    public static MessageQueue[] GetPublicQueuesByMachine(string machineName)
    {
      if (!SyntaxCheck.CheckMachineName(machineName))
      {
        throw new ArgumentException(System.Messaging.Res.GetString("InvalidParameter", (object) "MachineName", (object) machineName));
      }
      else
      {
        new MessageQueuePermission(MessageQueuePermissionAccess.Browse, "*").Demand();
        try
        {
          new DirectoryServicesPermission(PermissionState.Unrestricted).Assert();
          SearchResult one1 = new DirectorySearcher(string.Format((IFormatProvider) CultureInfo.InvariantCulture, "(&(CN={0})(objectCategory=Computer))", new object[1]
          {
            (object) MessageQueue.ComputerName
          })).FindOne();
          if (one1 != null)
          {
            SearchResult one2 = new DirectorySearcher(one1.GetDirectoryEntry())
            {
              Filter = "(CN=msmq)"
            }.FindOne();
            if (one2 != null)
            {
              SearchResult searchResult;
              if (machineName != "." && string.Compare(machineName, MessageQueue.ComputerName, true, CultureInfo.InvariantCulture) != 0)
              {
                SearchResult one3 = new DirectorySearcher(string.Format((IFormatProvider) CultureInfo.InvariantCulture, "(&(CN={0})(objectCategory=Computer))", new object[1]
                {
                  (object) machineName
                })).FindOne();
                if (one3 == null)
                  return new MessageQueue[0];
                searchResult = new DirectorySearcher(one3.GetDirectoryEntry())
                {
                  Filter = "(CN=msmq)"
                }.FindOne();
                if (searchResult == null)
                  return new MessageQueue[0];
              }
              else
                searchResult = one2;
              SearchResultCollection all = new DirectorySearcher(searchResult.GetDirectoryEntry())
              {
                Filter = "(objectClass=mSMQQueue)",
                PropertiesToLoad = {
                  "Name"
                }
              }.FindAll();
              MessageQueue[] messageQueueArray = new MessageQueue[all.Count];
              for (int index = 0; index < messageQueueArray.Length; ++index)
              {
                string str = (string) all[index].Properties["Name"][0];
                messageQueueArray[index] = new MessageQueue(string.Format((IFormatProvider) CultureInfo.InvariantCulture, "{0}\\{1}", new object[2]
                {
                  (object) machineName,
                  (object) str
                }));
              }
              return messageQueueArray;
            }
          }
        }
        catch
        {
        }
        finally
        {
          CodeAccessPermission.RevertAssert();
        }
        return MessageQueue.CreateMessageQueuesSnapshot(new MessageQueueCriteria()
        {
          MachineName = machineName
        }, false);
      }
    }

    public Message Peek()
    {
      return this.ReceiveCurrent(MessageQueue.InfiniteTimeout, int.MaxValue, CursorHandle.NullHandle, this.MessageReadPropertyFilter, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public Message Peek(TimeSpan timeout)
    {
      return this.ReceiveCurrent(timeout, int.MaxValue, CursorHandle.NullHandle, this.MessageReadPropertyFilter, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public Message Peek(TimeSpan timeout, Cursor cursor, PeekAction action)
    {
      if (action != PeekAction.Current && action != PeekAction.Next)
        throw new ArgumentOutOfRangeException(System.Messaging.Res.GetString("InvalidParameter", (object) "action", (object) ((object) action).ToString()));
      else if (cursor == null)
        throw new ArgumentNullException("cursor");
      else
        return this.ReceiveCurrent(timeout, (int) action, cursor.Handle, this.MessageReadPropertyFilter, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public Message PeekById(string id)
    {
      return this.ReceiveBy(id, TimeSpan.Zero, false, true, false, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public Message PeekById(string id, TimeSpan timeout)
    {
      return this.ReceiveBy(id, timeout, false, true, true, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public Message PeekByCorrelationId(string correlationId)
    {
      return this.ReceiveBy(correlationId, TimeSpan.Zero, false, false, false, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public Message PeekByCorrelationId(string correlationId, TimeSpan timeout)
    {
      return this.ReceiveBy(correlationId, timeout, false, false, true, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public void Purge()
    {
      if (!this.receiveGranted)
      {
        new MessageQueuePermission((MessageQueuePermissionAccess) 26, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
        this.receiveGranted = true;
      }
      int error = this.StaleSafePurgeQueue();
      if (MessageQueue.IsFatalError(error))
        throw new MessageQueueException(error);
    }

    public Message Receive()
    {
      return this.ReceiveCurrent(MessageQueue.InfiniteTimeout, 0, CursorHandle.NullHandle, this.MessageReadPropertyFilter, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public Message Receive(MessageQueueTransaction transaction)
    {
      if (transaction == null)
        throw new ArgumentNullException("transaction");
      else
        return this.ReceiveCurrent(MessageQueue.InfiniteTimeout, 0, CursorHandle.NullHandle, this.MessageReadPropertyFilter, transaction, MessageQueueTransactionType.None);
    }

    public Message Receive(MessageQueueTransactionType transactionType)
    {
      if (!ValidationUtility.ValidateMessageQueueTransactionType(transactionType))
        throw new InvalidEnumArgumentException("transactionType", (int) transactionType, typeof (MessageQueueTransactionType));
      else
        return this.ReceiveCurrent(MessageQueue.InfiniteTimeout, 0, CursorHandle.NullHandle, this.MessageReadPropertyFilter, (MessageQueueTransaction) null, transactionType);
    }

    public Message Receive(TimeSpan timeout)
    {
      return this.ReceiveCurrent(timeout, 0, CursorHandle.NullHandle, this.MessageReadPropertyFilter, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public Message Receive(TimeSpan timeout, Cursor cursor)
    {
      if (cursor == null)
        throw new ArgumentNullException("cursor");
      else
        return this.ReceiveCurrent(timeout, 0, cursor.Handle, this.MessageReadPropertyFilter, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public Message Receive(TimeSpan timeout, MessageQueueTransaction transaction)
    {
      if (transaction == null)
        throw new ArgumentNullException("transaction");
      else
        return this.ReceiveCurrent(timeout, 0, CursorHandle.NullHandle, this.MessageReadPropertyFilter, transaction, MessageQueueTransactionType.None);
    }

    public Message Receive(TimeSpan timeout, MessageQueueTransactionType transactionType)
    {
      if (!ValidationUtility.ValidateMessageQueueTransactionType(transactionType))
        throw new InvalidEnumArgumentException("transactionType", (int) transactionType, typeof (MessageQueueTransactionType));
      else
        return this.ReceiveCurrent(timeout, 0, CursorHandle.NullHandle, this.MessageReadPropertyFilter, (MessageQueueTransaction) null, transactionType);
    }

    public Message Receive(TimeSpan timeout, Cursor cursor, MessageQueueTransaction transaction)
    {
      if (transaction == null)
        throw new ArgumentNullException("transaction");
      if (cursor == null)
        throw new ArgumentNullException("cursor");
      else
        return this.ReceiveCurrent(timeout, 0, cursor.Handle, this.MessageReadPropertyFilter, transaction, MessageQueueTransactionType.None);
    }

    public Message Receive(TimeSpan timeout, Cursor cursor, MessageQueueTransactionType transactionType)
    {
      if (!ValidationUtility.ValidateMessageQueueTransactionType(transactionType))
        throw new InvalidEnumArgumentException("transactionType", (int) transactionType, typeof (MessageQueueTransactionType));
      if (cursor == null)
        throw new ArgumentNullException("cursor");
      else
        return this.ReceiveCurrent(timeout, 0, cursor.Handle, this.MessageReadPropertyFilter, (MessageQueueTransaction) null, transactionType);
    }

    private Message ReceiveBy(string id, TimeSpan timeout, bool remove, bool compareId, bool throwTimeout, MessageQueueTransaction transaction, MessageQueueTransactionType transactionType)
    {
      if (id == null)
        throw new ArgumentNullException("id");
      if (timeout < TimeSpan.Zero || timeout > MessageQueue.InfiniteTimeout)
      {
        throw new ArgumentException(System.Messaging.Res.GetString("InvalidParameter", (object) "timeout", (object) timeout.ToString()));
      }
      else
      {
        MessagePropertyFilter messagePropertyFilter = this.receiveFilter;
        CursorHandle cursorHandle = (CursorHandle) null;
        try
        {
          this.receiveFilter = new MessagePropertyFilter();
          this.receiveFilter.ClearAll();
          if (!compareId)
            this.receiveFilter.CorrelationId = true;
          else
            this.receiveFilter.Id = true;
          int cursor = SafeNativeMethods.MQCreateCursor(this.MQInfo.ReadHandle, out cursorHandle);
          if (MessageQueue.IsFatalError(cursor))
            throw new MessageQueueException(cursor);
          try
          {
            for (Message message = this.ReceiveCurrent(timeout, int.MaxValue, cursorHandle, this.MessageReadPropertyFilter, (MessageQueueTransaction) null, MessageQueueTransactionType.None); message != null; message = this.ReceiveCurrent(timeout, -2147483647, cursorHandle, this.MessageReadPropertyFilter, (MessageQueueTransaction) null, MessageQueueTransactionType.None))
            {
              if (compareId && string.Compare(message.Id, id, true, CultureInfo.InvariantCulture) == 0 || !compareId && string.Compare(message.CorrelationId, id, true, CultureInfo.InvariantCulture) == 0)
              {
                this.receiveFilter = messagePropertyFilter;
                if (remove && transaction == null)
                  return this.ReceiveCurrent(timeout, 0, cursorHandle, this.MessageReadPropertyFilter, (MessageQueueTransaction) null, transactionType);
                else
                  return this.ReceiveCurrent(timeout, int.MaxValue, cursorHandle, this.MessageReadPropertyFilter, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
              }
            }
          }
          catch (MessageQueueException ex)
          {
          }
        }
        finally
        {
          this.receiveFilter = messagePropertyFilter;
          if (cursorHandle != null)
            cursorHandle.Close();
        }
        if (!throwTimeout)
          throw new InvalidOperationException(System.Messaging.Res.GetString("MessageNotFound"));
        else
          throw new MessageQueueException(-1072824293);
      }
    }

    public Message ReceiveById(string id)
    {
      return this.ReceiveBy(id, TimeSpan.Zero, true, true, false, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public Message ReceiveById(string id, MessageQueueTransaction transaction)
    {
      if (transaction == null)
        throw new ArgumentNullException("transaction");
      else
        return this.ReceiveBy(id, TimeSpan.Zero, true, true, false, transaction, MessageQueueTransactionType.None);
    }

    public Message ReceiveById(string id, MessageQueueTransactionType transactionType)
    {
      if (!ValidationUtility.ValidateMessageQueueTransactionType(transactionType))
        throw new InvalidEnumArgumentException("transactionType", (int) transactionType, typeof (MessageQueueTransactionType));
      else
        return this.ReceiveBy(id, TimeSpan.Zero, true, true, false, (MessageQueueTransaction) null, transactionType);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public Message ReceiveById(string id, TimeSpan timeout)
    {
      return this.ReceiveBy(id, timeout, true, true, true, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public Message ReceiveById(string id, TimeSpan timeout, MessageQueueTransaction transaction)
    {
      if (transaction == null)
        throw new ArgumentNullException("transaction");
      else
        return this.ReceiveBy(id, timeout, true, true, true, transaction, MessageQueueTransactionType.None);
    }

    public Message ReceiveById(string id, TimeSpan timeout, MessageQueueTransactionType transactionType)
    {
      if (!ValidationUtility.ValidateMessageQueueTransactionType(transactionType))
        throw new InvalidEnumArgumentException("transactionType", (int) transactionType, typeof (MessageQueueTransactionType));
      else
        return this.ReceiveBy(id, timeout, true, true, true, (MessageQueueTransaction) null, transactionType);
    }

    public Message ReceiveByCorrelationId(string correlationId)
    {
      return this.ReceiveBy(correlationId, TimeSpan.Zero, true, false, false, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public Message ReceiveByCorrelationId(string correlationId, MessageQueueTransaction transaction)
    {
      if (transaction == null)
        throw new ArgumentNullException("transaction");
      else
        return this.ReceiveBy(correlationId, TimeSpan.Zero, true, false, false, transaction, MessageQueueTransactionType.None);
    }

    public Message ReceiveByCorrelationId(string correlationId, MessageQueueTransactionType transactionType)
    {
      if (!ValidationUtility.ValidateMessageQueueTransactionType(transactionType))
        throw new InvalidEnumArgumentException("transactionType", (int) transactionType, typeof (MessageQueueTransactionType));
      else
        return this.ReceiveBy(correlationId, TimeSpan.Zero, true, false, false, (MessageQueueTransaction) null, transactionType);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public Message ReceiveByCorrelationId(string correlationId, TimeSpan timeout)
    {
      return this.ReceiveBy(correlationId, timeout, true, false, true, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public Message ReceiveByCorrelationId(string correlationId, TimeSpan timeout, MessageQueueTransaction transaction)
    {
      if (transaction == null)
        throw new ArgumentNullException("transaction");
      else
        return this.ReceiveBy(correlationId, timeout, true, false, true, transaction, MessageQueueTransactionType.None);
    }

    public Message ReceiveByCorrelationId(string correlationId, TimeSpan timeout, MessageQueueTransactionType transactionType)
    {
      if (!ValidationUtility.ValidateMessageQueueTransactionType(transactionType))
        throw new InvalidEnumArgumentException("transactionType", (int) transactionType, typeof (MessageQueueTransactionType));
      else
        return this.ReceiveBy(correlationId, timeout, true, false, true, (MessageQueueTransaction) null, transactionType);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public Message ReceiveByLookupId(long lookupId)
    {
      return this.InternalReceiveByLookupId(true, MessageLookupAction.Current, lookupId, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public Message ReceiveByLookupId(MessageLookupAction action, long lookupId, MessageQueueTransactionType transactionType)
    {
      return this.InternalReceiveByLookupId(true, action, lookupId, (MessageQueueTransaction) null, transactionType);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public Message ReceiveByLookupId(MessageLookupAction action, long lookupId, MessageQueueTransaction transaction)
    {
      return this.InternalReceiveByLookupId(true, action, lookupId, transaction, MessageQueueTransactionType.None);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public Message PeekByLookupId(long lookupId)
    {
      return this.InternalReceiveByLookupId(false, MessageLookupAction.Current, lookupId, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public Message PeekByLookupId(MessageLookupAction action, long lookupId)
    {
      return this.InternalReceiveByLookupId(false, action, lookupId, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    internal unsafe Message InternalReceiveByLookupId(bool receive, MessageLookupAction lookupAction, long lookupId, MessageQueueTransaction internalTransaction, MessageQueueTransactionType transactionType)
    {
      if (!ValidationUtility.ValidateMessageQueueTransactionType(transactionType))
        throw new InvalidEnumArgumentException("transactionType", (int) transactionType, typeof (MessageQueueTransactionType));
      if (!ValidationUtility.ValidateMessageLookupAction(lookupAction))
        throw new InvalidEnumArgumentException("action", (int) lookupAction, typeof (MessageLookupAction));
      if (!MessageQueue.Msmq3OrNewer)
        throw new PlatformNotSupportedException(System.Messaging.Res.GetString("PlatformNotSupported"));
      int action;
      if (receive)
      {
        if (!this.receiveGranted)
        {
          new MessageQueuePermission((MessageQueuePermissionAccess) 26, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
          this.receiveGranted = true;
        }
        action = (int) ((MessageLookupAction) 1073741856 | lookupAction);
      }
      else
      {
        if (!this.peekGranted)
        {
          new MessageQueuePermission((MessageQueuePermissionAccess) 10, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
          this.peekGranted = true;
        }
        action = (int) ((MessageLookupAction) 1073741840 | lookupAction);
      }
      MessagePropertyFilter readPropertyFilter = this.MessageReadPropertyFilter;
      int error = 0;
      Message message = (Message) null;
      MessagePropertyVariants.MQPROPS properties1 = (MessagePropertyVariants.MQPROPS) null;
      if (readPropertyFilter != null)
      {
        message = new Message((MessagePropertyFilter) readPropertyFilter.Clone());
        message.SetLookupId(lookupId);
        if (this.formatter != null)
          message.Formatter = (IMessageFormatter) this.formatter.Clone();
        properties1 = message.Lock();
      }
      try
      {
        error = internalTransaction == null || !receive ? this.StaleSafeReceiveByLookupId(lookupId, action, properties1, (NativeOverlapped*) null, (SafeNativeMethods.ReceiveCallback) null, (IntPtr) ((long) transactionType)) : this.StaleSafeReceiveByLookupId(lookupId, action, properties1, (NativeOverlapped*) null, (SafeNativeMethods.ReceiveCallback) null, internalTransaction.BeginQueueOperation());
        if (message != null)
        {
          for (; MessageQueue.IsMemoryError(error); {
            MessagePropertyVariants.MQPROPS properties2;
            error = internalTransaction == null || !receive ? this.StaleSafeReceiveByLookupId(lookupId, action, properties2, (NativeOverlapped*) null, (SafeNativeMethods.ReceiveCallback) null, (IntPtr) ((long) transactionType)) : this.StaleSafeReceiveByLookupId(lookupId, action, properties2, (NativeOverlapped*) null, (SafeNativeMethods.ReceiveCallback) null, internalTransaction.InnerTransaction);
          }
          )
          {
            message.Unlock();
            message.AdjustMemory();
            properties2 = message.Lock();
          }
          message.Unlock();
        }
      }
      finally
      {
        if (internalTransaction != null && receive)
          internalTransaction.EndQueueOperation();
      }
      if (error == -1072824184)
        throw new InvalidOperationException(System.Messaging.Res.GetString("MessageNotFound"));
      if (MessageQueue.IsFatalError(error))
        throw new MessageQueueException(error);
      else
        return message;
    }

    public void Refresh()
    {
      this.PropertyFilter.ClearAll();
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public void Send(object obj)
    {
      this.SendInternal(obj, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public void Send(object obj, MessageQueueTransaction transaction)
    {
      if (transaction == null)
        throw new ArgumentNullException("transaction");
      this.SendInternal(obj, transaction, MessageQueueTransactionType.None);
    }

    public void Send(object obj, MessageQueueTransactionType transactionType)
    {
      if (!ValidationUtility.ValidateMessageQueueTransactionType(transactionType))
        throw new InvalidEnumArgumentException("transactionType", (int) transactionType, typeof (MessageQueueTransactionType));
      this.SendInternal(obj, (MessageQueueTransaction) null, transactionType);
    }

    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    public void Send(object obj, string label)
    {
      this.Send(obj, label, (MessageQueueTransaction) null, MessageQueueTransactionType.None);
    }

    public void Send(object obj, string label, MessageQueueTransaction transaction)
    {
      if (transaction == null)
        throw new ArgumentNullException("transaction");
      this.Send(obj, label, transaction, MessageQueueTransactionType.None);
    }

    public void Send(object obj, string label, MessageQueueTransactionType transactionType)
    {
      if (!ValidationUtility.ValidateMessageQueueTransactionType(transactionType))
        throw new InvalidEnumArgumentException("transactionType", (int) transactionType, typeof (MessageQueueTransactionType));
      this.Send(obj, label, (MessageQueueTransaction) null, transactionType);
    }

    private void Send(object obj, string label, MessageQueueTransaction transaction, MessageQueueTransactionType transactionType)
    {
      if (label == null)
        throw new ArgumentNullException("label");
      if (obj is Message)
      {
        ((Message) obj).Label = label;
        this.SendInternal(obj, transaction, transactionType);
      }
      else
      {
        string label1 = this.DefaultPropertiesToSend.Label;
        try
        {
          this.DefaultPropertiesToSend.Label = label;
          this.SendInternal(obj, transaction, transactionType);
        }
        finally
        {
          this.DefaultPropertiesToSend.Label = label1;
        }
      }
    }

    private void SendInternal(object obj, MessageQueueTransaction internalTransaction, MessageQueueTransactionType transactionType)
    {
      if (!this.sendGranted)
      {
        new MessageQueuePermission((MessageQueuePermissionAccess) 6, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
        this.sendGranted = true;
      }
      Message message = (Message) null;
      if (obj is Message)
        message = (Message) obj;
      if (message == null)
      {
        message = this.DefaultPropertiesToSend.CachedMessage;
        message.Formatter = this.Formatter;
        message.Body = obj;
      }
      int error = 0;
      message.AdjustToSend();
      MessagePropertyVariants.MQPROPS properties = message.Lock();
      try
      {
        error = internalTransaction == null ? this.StaleSafeSendMessage(properties, (IntPtr) ((long) transactionType)) : this.StaleSafeSendMessage(properties, internalTransaction.BeginQueueOperation());
      }
      finally
      {
        message.Unlock();
        if (internalTransaction != null)
          internalTransaction.EndQueueOperation();
      }
      if (MessageQueue.IsFatalError(error))
        throw new MessageQueueException(error);
    }

    public void ResetPermissions()
    {
      if (!this.administerGranted)
      {
        new MessageQueuePermission((MessageQueuePermissionAccess) 62, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
        this.administerGranted = true;
      }
      int error = System.Messaging.Interop.UnsafeNativeMethods.MQSetQueueSecurity(this.FormatName, 4, (System.Messaging.Interop.NativeMethods.SECURITY_DESCRIPTOR) null);
      if (error != 0)
        throw new MessageQueueException(error);
    }

    public void SetPermissions(string user, MessageQueueAccessRights rights)
    {
      if (user == null)
        throw new ArgumentNullException("user");
      this.SetPermissions(user, rights, AccessControlEntryType.Allow);
    }

    public void SetPermissions(string user, MessageQueueAccessRights rights, AccessControlEntryType entryType)
    {
      if (user == null)
        throw new ArgumentNullException("user");
      this.SetPermissions(new AccessControlList()
      {
        (AccessControlEntry) new MessageQueueAccessControlEntry(new Trustee(user), rights, entryType)
      });
    }

    public void SetPermissions(MessageQueueAccessControlEntry ace)
    {
      if (ace == null)
        throw new ArgumentNullException("ace");
      this.SetPermissions(new AccessControlList()
      {
        (AccessControlEntry) ace
      });
    }

    public void SetPermissions(AccessControlList dacl)
    {
      if (dacl == null)
        throw new ArgumentNullException("dacl");
      if (!this.administerGranted)
      {
        new MessageQueuePermission((MessageQueuePermissionAccess) 62, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
        this.administerGranted = true;
      }
      AccessControlList.CheckEnvironment();
      byte[] numArray1 = new byte[100];
      int lengthNeeded = 0;
      GCHandle gcHandle = GCHandle.Alloc((object) numArray1, GCHandleType.Pinned);
      try
      {
        int queueSecurity = System.Messaging.Interop.UnsafeNativeMethods.MQGetQueueSecurity(this.FormatName, 4, gcHandle.AddrOfPinnedObject(), numArray1.Length, out lengthNeeded);
        if (queueSecurity == -1072824285)
        {
          gcHandle.Free();
          byte[] numArray2 = new byte[lengthNeeded];
          gcHandle = GCHandle.Alloc((object) numArray2, GCHandleType.Pinned);
          queueSecurity = System.Messaging.Interop.UnsafeNativeMethods.MQGetQueueSecurity(this.FormatName, 4, gcHandle.AddrOfPinnedObject(), numArray2.Length, out lengthNeeded);
        }
        if (queueSecurity != 0)
          throw new MessageQueueException(queueSecurity);
        bool daclPresent;
        IntPtr pDacl;
        bool daclDefaulted;
        if (!System.Messaging.Interop.UnsafeNativeMethods.GetSecurityDescriptorDacl(gcHandle.AddrOfPinnedObject(), out daclPresent, out pDacl, out daclDefaulted))
          throw new Win32Exception();
        System.Messaging.Interop.NativeMethods.SECURITY_DESCRIPTOR securityDescriptor = new System.Messaging.Interop.NativeMethods.SECURITY_DESCRIPTOR();
        System.Messaging.Interop.UnsafeNativeMethods.InitializeSecurityDescriptor(securityDescriptor, 1);
        IntPtr num = dacl.MakeAcl(pDacl);
        try
        {
          if (!System.Messaging.Interop.UnsafeNativeMethods.SetSecurityDescriptorDacl(securityDescriptor, true, num, false))
            throw new Win32Exception();
          int error = System.Messaging.Interop.UnsafeNativeMethods.MQSetQueueSecurity(this.FormatName, 4, securityDescriptor);
          if (error != 0)
            throw new MessageQueueException(error);
        }
        finally
        {
          AccessControlList.FreeAcl(num);
        }
        MessageQueue.queueInfoCache.Remove(this.QueueInfoKey);
        MessageQueue.formatNameCache.Remove(this.path.ToUpper(CultureInfo.InvariantCulture));
      }
      finally
      {
        if (gcHandle.IsAllocated)
          gcHandle.Free();
      }
    }

    private void GenerateQueueProperties()
    {
      if (!this.browseGranted)
      {
        new MessageQueuePermission(MessageQueuePermissionAccess.Browse, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
        this.browseGranted = true;
      }
      int queueProperties = System.Messaging.Interop.UnsafeNativeMethods.MQGetQueueProperties(this.FormatName, this.Properties.Lock());
      this.Properties.Unlock();
      if (MessageQueue.IsFatalError(queueProperties))
        throw new MessageQueueException(queueProperties);
    }

    internal static MessageQueueEnumerator GetMessageQueueEnumerator(MessageQueueCriteria criteria, bool checkSecurity)
    {
      return new MessageQueueEnumerator(criteria, checkSecurity);
    }

    private static bool IsCanonicalPath(string path, bool checkQueueNameSize)
    {
      if (!MessageQueue.ValidatePath(path, checkQueueNameSize))
        return false;
      string str = path.ToUpper(CultureInfo.InvariantCulture);
      if (str.StartsWith(MessageQueue.PREFIX_LABEL) || str.StartsWith(MessageQueue.PREFIX_FORMAT_NAME) || (str.EndsWith(MessageQueue.SUFIX_DEADLETTER) || str.EndsWith(MessageQueue.SUFIX_DEADXACT)) || str.EndsWith(MessageQueue.SUFIX_JOURNAL))
        return false;
      else
        return true;
    }

    internal static bool IsFatalError(int value)
    {
      bool flag = value == 0;
      if ((value & -1073741824) != 1073741824)
        return !flag;
      else
        return false;
    }

    internal static bool IsMemoryError(int value)
    {
      if (value == -1072824294 || value == -1072824226 || (value == -1072824221 || value == -1072824277) || (value == -1072824286 || value == -1072824285 || (value == -1072824222 || value == -1072824223)) || (value == -1072824280 || value == -1072824289))
        return true;
      else
        return false;
    }

    private void OnRequestCompleted(IAsyncResult asyncResult)
    {
      if (((MessageQueue.AsynchronousRequest) asyncResult).Action == int.MaxValue)
      {
        if (this.onPeekCompleted == null)
          return;
        this.onPeekCompleted((object) this, new PeekCompletedEventArgs(this, asyncResult));
      }
      else
      {
        if (this.onReceiveCompleted == null)
          return;
        this.onReceiveCompleted((object) this, new ReceiveCompletedEventArgs(this, asyncResult));
      }
    }

    private IAsyncResult ReceiveAsync(TimeSpan timeout, CursorHandle cursorHandle, int action, AsyncCallback callback, object stateObject)
    {
      long num = (long) timeout.TotalMilliseconds;
      if (num < 0L || num > (long) uint.MaxValue)
      {
        throw new ArgumentException(System.Messaging.Res.GetString("InvalidParameter", (object) "timeout", (object) timeout.ToString()));
      }
      else
      {
        if (action == 0)
        {
          if (!this.receiveGranted)
          {
            new MessageQueuePermission((MessageQueuePermissionAccess) 26, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
            this.receiveGranted = true;
          }
        }
        else if (!this.peekGranted)
        {
          new MessageQueuePermission((MessageQueuePermissionAccess) 10, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
          this.peekGranted = true;
        }
        if (!this.attached)
        {
          lock (this)
          {
            if (!this.attached)
            {
              int local_4;
              if (!SafeNativeMethods.GetHandleInformation((SafeHandle) this.MQInfo.ReadHandle, out local_4))
              {
                this.useThreadPool = false;
              }
              else
              {
                this.MQInfo.BindToThreadPool();
                this.useThreadPool = true;
              }
              this.attached = true;
            }
          }
        }
        if (callback == null)
        {
          if (this.onRequestCompleted == null)
            this.onRequestCompleted = new AsyncCallback(this.OnRequestCompleted);
          callback = this.onRequestCompleted;
        }
        MessageQueue.AsynchronousRequest asynchronousRequest = new MessageQueue.AsynchronousRequest(this, (uint) num, cursorHandle, action, this.useThreadPool, stateObject, callback);
        if (!this.useThreadPool)
          this.OutstandingAsyncRequests[(object) asynchronousRequest] = (object) asynchronousRequest;
        asynchronousRequest.BeginRead();
        return (IAsyncResult) asynchronousRequest;
      }
    }

    internal unsafe Message ReceiveCurrent(TimeSpan timeout, int action, CursorHandle cursor, MessagePropertyFilter filter, MessageQueueTransaction internalTransaction, MessageQueueTransactionType transactionType)
    {
      long num = (long) timeout.TotalMilliseconds;
      if (num < 0L || num > (long) uint.MaxValue)
      {
        throw new ArgumentException(System.Messaging.Res.GetString("InvalidParameter", (object) "timeout", (object) timeout.ToString()));
      }
      else
      {
        if (action == 0)
        {
          if (!this.receiveGranted)
          {
            new MessageQueuePermission((MessageQueuePermissionAccess) 26, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
            this.receiveGranted = true;
          }
        }
        else if (!this.peekGranted)
        {
          new MessageQueuePermission((MessageQueuePermissionAccess) 10, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
          this.peekGranted = true;
        }
        int error = 0;
        Message message = (Message) null;
        MessagePropertyVariants.MQPROPS properties1 = (MessagePropertyVariants.MQPROPS) null;
        if (filter != null)
        {
          message = new Message((MessagePropertyFilter) filter.Clone());
          if (this.formatter != null)
            message.Formatter = (IMessageFormatter) this.formatter.Clone();
          properties1 = message.Lock();
        }
        try
        {
          error = internalTransaction == null ? this.StaleSafeReceiveMessage((uint) num, action, properties1, (NativeOverlapped*) null, (SafeNativeMethods.ReceiveCallback) null, cursor, (IntPtr) ((long) transactionType)) : this.StaleSafeReceiveMessage((uint) num, action, properties1, (NativeOverlapped*) null, (SafeNativeMethods.ReceiveCallback) null, cursor, internalTransaction.BeginQueueOperation());
          if (message != null)
          {
            for (; MessageQueue.IsMemoryError(error); {
              MessagePropertyVariants.MQPROPS properties2;
              error = internalTransaction == null ? this.StaleSafeReceiveMessage((uint) num, action, properties2, (NativeOverlapped*) null, (SafeNativeMethods.ReceiveCallback) null, cursor, (IntPtr) ((long) transactionType)) : this.StaleSafeReceiveMessage((uint) num, action, properties2, (NativeOverlapped*) null, (SafeNativeMethods.ReceiveCallback) null, cursor, internalTransaction.InnerTransaction);
            }
            )
            {
              if (action == -2147483647)
                action = int.MaxValue;
              message.Unlock();
              message.AdjustMemory();
              properties2 = message.Lock();
            }
          }
        }
        finally
        {
          if (message != null)
            message.Unlock();
          if (internalTransaction != null)
            internalTransaction.EndQueueOperation();
        }
        if (MessageQueue.IsFatalError(error))
          throw new MessageQueueException(error);
        else
          return message;
      }
    }

    private void SaveQueueProperties()
    {
      if (!this.administerGranted)
      {
        new MessageQueuePermission((MessageQueuePermissionAccess) 62, MessageQueue.PREFIX_FORMAT_NAME + this.FormatName).Demand();
        this.administerGranted = true;
      }
      int error = System.Messaging.Interop.UnsafeNativeMethods.MQSetQueueProperties(this.FormatName, this.Properties.Lock());
      this.Properties.Unlock();
      if (MessageQueue.IsFatalError(error))
        throw new MessageQueueException(error);
    }

    private static MessageQueue ResolveQueueFromLabel(string path, bool throwException)
    {
      MessageQueue[] publicQueuesByLabel = MessageQueue.GetPublicQueuesByLabel(path.Substring(MessageQueue.PREFIX_LABEL.Length), false);
      if (publicQueuesByLabel.Length == 0)
      {
        if (!throwException)
          return (MessageQueue) null;
        throw new InvalidOperationException(System.Messaging.Res.GetString("InvalidLabel", new object[1]
        {
          (object) path.Substring(MessageQueue.PREFIX_LABEL.Length)
        }));
      }
      else
      {
        if (publicQueuesByLabel.Length <= 1)
          return publicQueuesByLabel[0];
        throw new InvalidOperationException(System.Messaging.Res.GetString("AmbiguousLabel", new object[1]
        {
          (object) path.Substring(MessageQueue.PREFIX_LABEL.Length)
        }));
      }
    }

    private static string ResolveFormatNameFromQueuePath(string queuePath, bool throwException)
    {
      string machineName = queuePath.Substring(0, queuePath.IndexOf('\\'));
      string strA = queuePath.Substring(queuePath.IndexOf('\\'));
      if (string.Compare(strA, MessageQueue.SUFIX_DEADLETTER, true, CultureInfo.InvariantCulture) == 0 || string.Compare(strA, MessageQueue.SUFIX_DEADXACT, true, CultureInfo.InvariantCulture) == 0 || string.Compare(strA, MessageQueue.SUFIX_JOURNAL, true, CultureInfo.InvariantCulture) == 0)
      {
        if (machineName.CompareTo(".") == 0)
          machineName = MessageQueue.ComputerName;
        Guid machineId = MessageQueue.GetMachineId(machineName);
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("MACHINE=");
        stringBuilder.Append(machineId.ToString());
        if (string.Compare(strA, MessageQueue.SUFIX_DEADXACT, true, CultureInfo.InvariantCulture) == 0)
          stringBuilder.Append(";DEADXACT");
        else if (string.Compare(strA, MessageQueue.SUFIX_DEADLETTER, true, CultureInfo.InvariantCulture) == 0)
          stringBuilder.Append(";DEADLETTER");
        else
          stringBuilder.Append(";JOURNAL");
        return ((object) stringBuilder).ToString();
      }
      else
      {
        string pathName = queuePath;
        bool flag = false;
        if (queuePath.ToUpper(CultureInfo.InvariantCulture).EndsWith(MessageQueue.SUFIX_JOURNAL))
        {
          flag = true;
          int length = pathName.LastIndexOf('\\');
          pathName = pathName.Substring(0, length);
        }
        StringBuilder formatName = new StringBuilder(124);
        int count = 124;
        int error = SafeNativeMethods.MQPathNameToFormatName(pathName, formatName, ref count);
        if (error != 0)
        {
          if (throwException)
            throw new MessageQueueException(error);
          if (error == -1072824300)
            throw new MessageQueueException(error);
          else
            return (string) null;
        }
        else
        {
          if (flag)
            formatName.Append(";JOURNAL");
          return ((object) formatName).ToString();
        }
      }
    }

    internal static bool ValidatePath(string path, bool checkQueueNameSize)
    {
      if (path == null || path.Length == 0)
        return true;
      string str = path.ToUpper(CultureInfo.InvariantCulture);
      if (str.StartsWith(MessageQueue.PREFIX_LABEL) || str.StartsWith(MessageQueue.PREFIX_FORMAT_NAME))
        return true;
      int num1 = 0;
      int num2 = -1;
      while (true)
      {
        int num3 = str.IndexOf('\\', num2 + 1);
        if (num3 != -1)
        {
          num2 = num3;
          ++num1;
        }
        else
          break;
      }
      if (num1 == 1)
      {
        if (checkQueueNameSize && (long) (path.Length - (num2 + 1)) > (long) byte.MaxValue)
          throw new ArgumentException(System.Messaging.Res.GetString("LongQueueName"));
        else
          return true;
      }
      else if (num1 == 2 && (str.EndsWith(MessageQueue.SUFIX_JOURNAL) || str.LastIndexOf(MessageQueue.SUFIX_PRIVATE + "\\") != -1) || num1 == 3 && str.EndsWith(MessageQueue.SUFIX_JOURNAL) && str.LastIndexOf(MessageQueue.SUFIX_PRIVATE + "\\") != -1)
        return true;
      else
        return false;
    }

    internal void SetAccessMode(QueueAccessMode accessMode)
    {
      if (!ValidationUtility.ValidateQueueAccessMode(accessMode))
        throw new InvalidEnumArgumentException("accessMode", (int) accessMode, typeof (QueueAccessMode));
      this.accessMode = accessMode;
    }

    private int StaleSafePurgeQueue()
    {
      int num = System.Messaging.Interop.UnsafeNativeMethods.MQPurgeQueue(this.MQInfo.ReadHandle);
      switch (num)
      {
        case -1072824234:
        case -1072824230:
          this.MQInfo.Close();
          num = System.Messaging.Interop.UnsafeNativeMethods.MQPurgeQueue(this.MQInfo.ReadHandle);
          break;
      }
      return num;
    }

    private int StaleSafeSendMessage(MessagePropertyVariants.MQPROPS properties, IntPtr transaction)
    {
      if ((int) transaction == 1)
      {
        Transaction current = Transaction.Current;
        if (current != (Transaction) null)
        {
          IDtcTransaction dtcTransaction = TransactionInterop.GetDtcTransaction(current);
          return this.StaleSafeSendMessage(properties, (ITransaction) dtcTransaction);
        }
      }
      int num = System.Messaging.Interop.UnsafeNativeMethods.MQSendMessage(this.MQInfo.WriteHandle, properties, transaction);
      switch (num)
      {
        case -1072824234:
        case -1072824230:
          this.MQInfo.Close();
          num = System.Messaging.Interop.UnsafeNativeMethods.MQSendMessage(this.MQInfo.WriteHandle, properties, transaction);
          break;
      }
      return num;
    }

    private int StaleSafeSendMessage(MessagePropertyVariants.MQPROPS properties, ITransaction transaction)
    {
      int num = System.Messaging.Interop.UnsafeNativeMethods.MQSendMessage(this.MQInfo.WriteHandle, properties, transaction);
      switch (num)
      {
        case -1072824234:
        case -1072824230:
          this.MQInfo.Close();
          num = System.Messaging.Interop.UnsafeNativeMethods.MQSendMessage(this.MQInfo.WriteHandle, properties, transaction);
          break;
      }
      return num;
    }

    internal unsafe int StaleSafeReceiveMessage(uint timeout, int action, MessagePropertyVariants.MQPROPS properties, NativeOverlapped* overlapped, SafeNativeMethods.ReceiveCallback receiveCallback, CursorHandle cursorHandle, IntPtr transaction)
    {
      if ((int) transaction == 1)
      {
        Transaction current = Transaction.Current;
        if (current != (Transaction) null)
        {
          IDtcTransaction dtcTransaction = TransactionInterop.GetDtcTransaction(current);
          return this.StaleSafeReceiveMessage(timeout, action, properties, overlapped, receiveCallback, cursorHandle, (ITransaction) dtcTransaction);
        }
      }
      int receiveResult = System.Messaging.Interop.UnsafeNativeMethods.MQReceiveMessage(this.MQInfo.ReadHandle, timeout, action, properties, overlapped, receiveCallback, cursorHandle, transaction);
      if (this.IsCashedInfoInvalidOnReceive(receiveResult))
      {
        this.MQInfo.Close();
        receiveResult = System.Messaging.Interop.UnsafeNativeMethods.MQReceiveMessage(this.MQInfo.ReadHandle, timeout, action, properties, overlapped, receiveCallback, cursorHandle, transaction);
      }
      return receiveResult;
    }

    private unsafe int StaleSafeReceiveMessage(uint timeout, int action, MessagePropertyVariants.MQPROPS properties, NativeOverlapped* overlapped, SafeNativeMethods.ReceiveCallback receiveCallback, CursorHandle cursorHandle, ITransaction transaction)
    {
      int receiveResult = System.Messaging.Interop.UnsafeNativeMethods.MQReceiveMessage(this.MQInfo.ReadHandle, timeout, action, properties, overlapped, receiveCallback, cursorHandle, transaction);
      if (this.IsCashedInfoInvalidOnReceive(receiveResult))
      {
        this.MQInfo.Close();
        receiveResult = System.Messaging.Interop.UnsafeNativeMethods.MQReceiveMessage(this.MQInfo.ReadHandle, timeout, action, properties, overlapped, receiveCallback, cursorHandle, transaction);
      }
      return receiveResult;
    }

    private unsafe int StaleSafeReceiveByLookupId(long lookupId, int action, MessagePropertyVariants.MQPROPS properties, NativeOverlapped* overlapped, SafeNativeMethods.ReceiveCallback receiveCallback, IntPtr transaction)
    {
      if ((int) transaction == 1)
      {
        Transaction current = Transaction.Current;
        if (current != (Transaction) null)
        {
          IDtcTransaction dtcTransaction = TransactionInterop.GetDtcTransaction(current);
          return this.StaleSafeReceiveByLookupId(lookupId, action, properties, overlapped, receiveCallback, (ITransaction) dtcTransaction);
        }
      }
      int receiveResult = System.Messaging.Interop.UnsafeNativeMethods.MQReceiveMessageByLookupId(this.MQInfo.ReadHandle, lookupId, action, properties, overlapped, receiveCallback, transaction);
      if (this.IsCashedInfoInvalidOnReceive(receiveResult))
      {
        this.MQInfo.Close();
        receiveResult = System.Messaging.Interop.UnsafeNativeMethods.MQReceiveMessageByLookupId(this.MQInfo.ReadHandle, lookupId, action, properties, overlapped, receiveCallback, transaction);
      }
      return receiveResult;
    }

    private unsafe int StaleSafeReceiveByLookupId(long lookupId, int action, MessagePropertyVariants.MQPROPS properties, NativeOverlapped* overlapped, SafeNativeMethods.ReceiveCallback receiveCallback, ITransaction transaction)
    {
      int receiveResult = System.Messaging.Interop.UnsafeNativeMethods.MQReceiveMessageByLookupId(this.MQInfo.ReadHandle, lookupId, action, properties, overlapped, receiveCallback, transaction);
      if (this.IsCashedInfoInvalidOnReceive(receiveResult))
      {
        this.MQInfo.Close();
        receiveResult = System.Messaging.Interop.UnsafeNativeMethods.MQReceiveMessageByLookupId(this.MQInfo.ReadHandle, lookupId, action, properties, overlapped, receiveCallback, transaction);
      }
      return receiveResult;
    }

    private bool IsCashedInfoInvalidOnReceive(int receiveResult)
    {
      if (receiveResult != -1072824234 && receiveResult != -1072824313 && receiveResult != -1072824314)
        return receiveResult == -1072824230;
      else
        return true;
    }

    private class QueuePropertyFilter
    {
      public bool Authenticate;
      public bool BasePriority;
      public bool CreateTime;
      public bool EncryptionLevel;
      public bool Id;
      public bool Transactional;
      public bool Label;
      public bool LastModifyTime;
      public bool MaximumJournalSize;
      public bool MaximumQueueSize;
      public bool MulticastAddress;
      public bool Path;
      public bool Category;
      public bool UseJournalQueue;

      public void ClearAll()
      {
        this.Authenticate = false;
        this.BasePriority = false;
        this.CreateTime = false;
        this.EncryptionLevel = false;
        this.Id = false;
        this.Transactional = false;
        this.Label = false;
        this.LastModifyTime = false;
        this.MaximumJournalSize = false;
        this.MaximumQueueSize = false;
        this.Path = false;
        this.Category = false;
        this.UseJournalQueue = false;
        this.MulticastAddress = false;
      }
    }

    internal class CacheTable<Key, Value>
    {
      private Dictionary<Key, MessageQueue.CacheTable<Key, Value>.CacheEntry<Value>> table;
      private ReaderWriterLock rwLock;
      private string name;
      private int capacity;
      private int originalCapacity;
      private TimeSpan staleTime;

      public CacheTable(string name, int capacity, TimeSpan staleTime)
      {
        this.originalCapacity = capacity;
        this.capacity = capacity;
        this.staleTime = staleTime;
        this.name = name;
        this.rwLock = new ReaderWriterLock();
        this.table = new Dictionary<Key, MessageQueue.CacheTable<Key, Value>.CacheEntry<Value>>();
      }

      public Value Get(Key key)
      {
        Value obj = default (Value);
        this.rwLock.AcquireReaderLock(-1);
        try
        {
          if (this.table.ContainsKey(key))
          {
            MessageQueue.CacheTable<Key, Value>.CacheEntry<Value> cacheEntry = this.table[key];
            if (cacheEntry != null)
            {
              cacheEntry.timeStamp = DateTime.UtcNow;
              obj = cacheEntry.contents;
            }
          }
        }
        finally
        {
          this.rwLock.ReleaseReaderLock();
        }
        return obj;
      }

      public void Put(Key key, Value val)
      {
        this.rwLock.AcquireWriterLock(-1);
        try
        {
          if ((object) val == null)
          {
            this.table[key] = (MessageQueue.CacheTable<Key, Value>.CacheEntry<Value>) null;
          }
          else
          {
            MessageQueue.CacheTable<Key, Value>.CacheEntry<Value> cacheEntry = (MessageQueue.CacheTable<Key, Value>.CacheEntry<Value>) null;
            if (this.table.ContainsKey(key))
              cacheEntry = this.table[key];
            if (cacheEntry == null)
            {
              cacheEntry = new MessageQueue.CacheTable<Key, Value>.CacheEntry<Value>();
              this.table[key] = cacheEntry;
              if (this.table.Count >= this.capacity)
                this.ClearStale(this.staleTime);
            }
            cacheEntry.timeStamp = DateTime.UtcNow;
            cacheEntry.contents = val;
          }
        }
        finally
        {
          this.rwLock.ReleaseWriterLock();
        }
      }

      public void Remove(Key key)
      {
        this.rwLock.AcquireWriterLock(-1);
        try
        {
          if (!this.table.ContainsKey(key))
            return;
          this.table.Remove(key);
        }
        finally
        {
          this.rwLock.ReleaseWriterLock();
        }
      }

      public void ClearStale(TimeSpan staleAge)
      {
        DateTime utcNow = DateTime.UtcNow;
        Dictionary<Key, MessageQueue.CacheTable<Key, Value>.CacheEntry<Value>> dictionary = new Dictionary<Key, MessageQueue.CacheTable<Key, Value>.CacheEntry<Value>>();
        this.rwLock.AcquireReaderLock(-1);
        try
        {
          foreach (KeyValuePair<Key, MessageQueue.CacheTable<Key, Value>.CacheEntry<Value>> keyValuePair in this.table)
          {
            MessageQueue.CacheTable<Key, Value>.CacheEntry<Value> cacheEntry = keyValuePair.Value;
            if (utcNow - cacheEntry.timeStamp < staleAge)
              dictionary[keyValuePair.Key] = keyValuePair.Value;
          }
        }
        finally
        {
          this.rwLock.ReleaseReaderLock();
        }
        this.rwLock.AcquireWriterLock(-1);
        this.table = dictionary;
        this.capacity = 2 * this.table.Count;
        if (this.capacity < this.originalCapacity)
          this.capacity = this.originalCapacity;
        this.rwLock.ReleaseWriterLock();
      }

      private class CacheEntry<T>
      {
        public T contents;
        public DateTime timeStamp;

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public CacheEntry()
        {
        }
      }
    }

    internal class MQCacheableInfo
    {
      private volatile MessageQueueHandle readHandle = MessageQueueHandle.InvalidHandle;
      private volatile MessageQueueHandle writeHandle = MessageQueueHandle.InvalidHandle;
      private object syncRoot = new object();
      private bool isTransactional;
      private volatile bool isTransactional_valid;
      private volatile bool boundToThreadPool;
      private string formatName;
      private int shareMode;
      private QueueAccessModeHolder accessMode;
      private int refCount;
      private bool disposed;

      public bool CanRead
      {
        get
        {
          if (!this.accessMode.CanRead())
            return false;
          if (this.readHandle.IsInvalid)
          {
            if (this.disposed)
              throw new ObjectDisposedException(this.GetType().Name);
            lock (this.syncRoot)
            {
              if (this.readHandle.IsInvalid)
              {
                MessageQueueHandle local_0;
                if (MessageQueue.IsFatalError(System.Messaging.Interop.UnsafeNativeMethods.MQOpenQueue(this.formatName, this.accessMode.GetReadAccessMode(), this.shareMode, out local_0)))
                  return false;
                this.readHandle = local_0;
              }
            }
          }
          return true;
        }
      }

      public bool CanWrite
      {
        get
        {
          if (!this.accessMode.CanWrite())
            return false;
          if (this.writeHandle.IsInvalid)
          {
            if (this.disposed)
              throw new ObjectDisposedException(this.GetType().Name);
            lock (this.syncRoot)
            {
              if (this.writeHandle.IsInvalid)
              {
                MessageQueueHandle local_0;
                if (MessageQueue.IsFatalError(System.Messaging.Interop.UnsafeNativeMethods.MQOpenQueue(this.formatName, this.accessMode.GetWriteAccessMode(), 0, out local_0)))
                  return false;
                this.writeHandle = local_0;
              }
            }
          }
          return true;
        }
      }

      public int RefCount
      {
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")] get
        {
          return this.refCount;
        }
      }

      public MessageQueueHandle ReadHandle
      {
        get
        {
          if (this.readHandle.IsInvalid)
          {
            if (this.disposed)
              throw new ObjectDisposedException(this.GetType().Name);
            lock (this.syncRoot)
            {
              if (this.readHandle.IsInvalid)
              {
                MessageQueueHandle local_0;
                int local_1 = System.Messaging.Interop.UnsafeNativeMethods.MQOpenQueue(this.formatName, this.accessMode.GetReadAccessMode(), this.shareMode, out local_0);
                if (MessageQueue.IsFatalError(local_1))
                  throw new MessageQueueException(local_1);
                this.readHandle = local_0;
              }
            }
          }
          return this.readHandle;
        }
      }

      public MessageQueueHandle WriteHandle
      {
        get
        {
          if (this.writeHandle.IsInvalid)
          {
            if (this.disposed)
              throw new ObjectDisposedException(this.GetType().Name);
            lock (this.syncRoot)
            {
              if (this.writeHandle.IsInvalid)
              {
                MessageQueueHandle local_0;
                int local_1 = System.Messaging.Interop.UnsafeNativeMethods.MQOpenQueue(this.formatName, this.accessMode.GetWriteAccessMode(), 0, out local_0);
                if (MessageQueue.IsFatalError(local_1))
                  throw new MessageQueueException(local_1);
                this.writeHandle = local_0;
              }
            }
          }
          return this.writeHandle;
        }
      }

      public bool Transactional
      {
        get
        {
          if (!this.isTransactional_valid)
          {
            lock (this.syncRoot)
            {
              if (!this.isTransactional_valid)
              {
                QueuePropertyVariants local_0 = new QueuePropertyVariants();
                local_0.SetUI1(113, (byte) 0);
                int local_1 = System.Messaging.Interop.UnsafeNativeMethods.MQGetQueueProperties(this.formatName, local_0.Lock());
                local_0.Unlock();
                if (MessageQueue.IsFatalError(local_1))
                  throw new MessageQueueException(local_1);
                this.isTransactional = (int) local_0.GetUI1(113) != 0;
                this.isTransactional_valid = true;
              }
            }
          }
          return this.isTransactional;
        }
      }

      public MQCacheableInfo(string formatName, QueueAccessMode accessMode, int shareMode)
      {
        this.formatName = formatName;
        this.shareMode = shareMode;
        this.accessMode = QueueAccessModeHolder.GetQueueAccessModeHolder(accessMode);
      }

      ~MQCacheableInfo()
      {
        this.Dispose(false);
      }

      public void AddRef()
      {
        lock (this)
          ++this.refCount;
      }

      public void BindToThreadPool()
      {
        if (this.boundToThreadPool)
          return;
        lock (this)
        {
          if (this.boundToThreadPool)
            return;
          new SecurityPermission(PermissionState.Unrestricted).Assert();
          try
          {
            ThreadPool.BindHandle((SafeHandle) this.ReadHandle);
          }
          finally
          {
            CodeAccessPermission.RevertAssert();
          }
          this.boundToThreadPool = true;
        }
      }

      public void CloseIfNotReferenced()
      {
        lock (this)
        {
          if (this.RefCount != 0)
            return;
          this.Close();
        }
      }

      public void Close()
      {
        this.boundToThreadPool = false;
        if (!this.writeHandle.IsInvalid)
        {
          lock (this.syncRoot)
          {
            if (!this.writeHandle.IsInvalid)
              this.writeHandle.Close();
          }
        }
        if (this.readHandle.IsInvalid)
          return;
        lock (this.syncRoot)
        {
          if (this.readHandle.IsInvalid)
            return;
          this.readHandle.Close();
        }
      }

      public void Dispose()
      {
        this.Dispose(true);
        GC.SuppressFinalize((object) this);
      }

      protected virtual void Dispose(bool disposing)
      {
        if (disposing)
        {
          this.Close();
        }
        else
        {
          if (!this.writeHandle.IsInvalid)
            this.writeHandle.Close();
          if (!this.readHandle.IsInvalid)
            this.readHandle.Close();
        }
        this.disposed = true;
      }

      public void Release()
      {
        lock (this)
          --this.refCount;
      }
    }

    internal class QueueInfoKeyHolder
    {
      private string formatName;
      private QueueAccessMode accessMode;

      public QueueInfoKeyHolder(string formatName, QueueAccessMode accessMode)
      {
        this.formatName = formatName.ToUpper(CultureInfo.InvariantCulture);
        this.accessMode = accessMode;
      }

      public override int GetHashCode()
      {
        return (int) (this.formatName.GetHashCode() + this.accessMode);
      }

      public override bool Equals(object obj)
      {
        if (obj == null || this.GetType() != obj.GetType())
          return false;
        else
          return this.Equals((MessageQueue.QueueInfoKeyHolder) obj);
      }

      public bool Equals(MessageQueue.QueueInfoKeyHolder qik)
      {
        if (qik == null || this.accessMode != qik.accessMode)
          return false;
        else
          return this.formatName.Equals(qik.formatName);
      }
    }

    private class AsynchronousRequest : IAsyncResult
    {
      private IOCompletionCallback onCompletionStatusChanged;
      private SafeNativeMethods.ReceiveCallback onMessageReceived;
      private AsyncCallback callback;
      private ManualResetEvent resetEvent;
      private object asyncState;
      private MessageQueue owner;
      private bool isCompleted;
      private int status;
      private Message message;
      private int action;
      private uint timeout;
      private CursorHandle cursorHandle;

      internal int Action
      {
        get
        {
          return this.action;
        }
      }

      public object AsyncState
      {
        get
        {
          return this.asyncState;
        }
      }

      public WaitHandle AsyncWaitHandle
      {
        get
        {
          return (WaitHandle) this.resetEvent;
        }
      }

      public bool CompletedSynchronously
      {
        get
        {
          return false;
        }
      }

      public bool IsCompleted
      {
        get
        {
          return this.isCompleted;
        }
      }

      internal AsynchronousRequest(MessageQueue owner, uint timeout, CursorHandle cursorHandle, int action, bool useThreadPool, object asyncState, AsyncCallback callback)
      {
        this.owner = owner;
        this.asyncState = asyncState;
        this.callback = callback;
        this.action = action;
        this.timeout = timeout;
        this.resetEvent = new ManualResetEvent(false);
        this.cursorHandle = cursorHandle;
        if (!useThreadPool)
          this.onMessageReceived = new SafeNativeMethods.ReceiveCallback(this.OnMessageReceived);
        else
          this.onCompletionStatusChanged = new IOCompletionCallback(this.OnCompletionStatusChanged);
      }

      internal unsafe void BeginRead()
      {
        NativeOverlapped* nativeOverlappedPtr = (NativeOverlapped*) null;
        if (this.onCompletionStatusChanged != null)
          nativeOverlappedPtr = new Overlapped()
          {
            AsyncResult = ((IAsyncResult) this)
          }.Pack(this.onCompletionStatusChanged, (object) null);
        this.message = new Message(this.owner.MessageReadPropertyFilter);
        int result;
        try
        {
          for (result = this.owner.StaleSafeReceiveMessage(this.timeout, this.action, this.message.Lock(), nativeOverlappedPtr, this.onMessageReceived, this.cursorHandle, IntPtr.Zero); MessageQueue.IsMemoryError(result); result = this.owner.StaleSafeReceiveMessage(this.timeout, this.action, this.message.Lock(), nativeOverlappedPtr, this.onMessageReceived, this.cursorHandle, IntPtr.Zero))
          {
            if (this.action == -2147483647)
              this.action = int.MaxValue;
            this.message.Unlock();
            this.message.AdjustMemory();
          }
        }
        catch (Exception ex)
        {
          this.message.Unlock();
          if ((IntPtr) nativeOverlappedPtr != IntPtr.Zero)
            Overlapped.Free(nativeOverlappedPtr);
          if (!this.owner.useThreadPool)
            this.owner.OutstandingAsyncRequests.Remove((object) this);
          throw ex;
        }
        if (!MessageQueue.IsFatalError(result))
          return;
        this.RaiseCompletionEvent(result, nativeOverlappedPtr);
      }

      internal Message End()
      {
        this.resetEvent.WaitOne();
        if (MessageQueue.IsFatalError(this.status))
          throw new MessageQueueException(this.status);
        if (this.owner.formatter != null)
          this.message.Formatter = (IMessageFormatter) this.owner.formatter.Clone();
        return this.message;
      }

      private unsafe void OnCompletionStatusChanged(uint errorCode, uint numBytes, NativeOverlapped* overlappedPointer)
      {
        int result = 0;
        if ((int) errorCode != 0)
          result = (int) (long) overlappedPointer->InternalLow;
        this.RaiseCompletionEvent(result, overlappedPointer);
      }

      private unsafe void OnMessageReceived(int result, IntPtr handle, int timeout, int action, IntPtr propertiesPointer, NativeOverlapped* overlappedPointer, IntPtr cursorHandle)
      {
        this.RaiseCompletionEvent(result, overlappedPointer);
      }

      private unsafe void RaiseCompletionEvent(int result, NativeOverlapped* overlappedPointer)
      {
        if (MessageQueue.IsMemoryError(result))
        {
          for (; MessageQueue.IsMemoryError(result); result = this.owner.StaleSafeReceiveMessage(this.timeout, this.action, this.message.Lock(), overlappedPointer, this.onMessageReceived, this.cursorHandle, IntPtr.Zero))
          {
            if (this.action == -2147483647)
              this.action = int.MaxValue;
            this.message.Unlock();
            this.message.AdjustMemory();
          }
          if (!MessageQueue.IsFatalError(result))
            return;
        }
        this.message.Unlock();
        if (this.owner.IsCashedInfoInvalidOnReceive(result))
        {
          this.owner.MQInfo.Close();
          result = this.owner.StaleSafeReceiveMessage(this.timeout, this.action, this.message.Lock(), overlappedPointer, this.onMessageReceived, this.cursorHandle, IntPtr.Zero);
          if (!MessageQueue.IsFatalError(result))
            return;
        }
        this.status = result;
        if ((IntPtr) overlappedPointer != IntPtr.Zero)
          Overlapped.Free(overlappedPointer);
        this.isCompleted = true;
        this.resetEvent.Set();
        try
        {
          if (this.owner.SynchronizingObject != null && this.owner.SynchronizingObject.InvokeRequired)
            this.owner.SynchronizingObject.BeginInvoke((Delegate) this.callback, new object[1]
            {
              (object) this
            });
          else
            this.callback((IAsyncResult) this);
        }
        catch (Exception ex)
        {
        }
        finally
        {
          if (!this.owner.useThreadPool)
            this.owner.OutstandingAsyncRequests.Remove((object) this);
        }
      }
    }
  }
}
