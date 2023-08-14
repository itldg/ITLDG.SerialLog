using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ITLDG.SerialLog
{
    public partial class SerialLog : UserControl
    {
        private bool _LogEnable = true;
        [Description("启用记录")]
        public bool LogEnable
        {
            get { return _LogEnable; }
            set
            {
                _LogEnable = value;
                chkEnable.Checked = value;
            }
        }

        private bool _LogAutoScroll = true;
        [Description("是否自动滚动")]
        public bool LogAutoScroll
        {
            get { return _LogAutoScroll; }
            set
            {
                _LogAutoScroll = value;
                chkAutoScroll.Checked = value;
            }
        }

        private LogType _SerialLogType;
        [Description("串口日志类型")]
        public LogType SerialLogType
        {
            get { return _SerialLogType; }
            set
            {
                _SerialLogType = value;
                cmbLogType.SelectedIndex = (int)value;
            }
        }

        private string _SerialLogChineseFontFamily = "Microsoft YaHei";
        [Description("串口日志中文字体")]
        public string SerialLogChineseFontFamily
        {
            get { return _SerialLogChineseFontFamily; }
            set
            {
                _SerialLogChineseFontFamily = value;
            }
        }

        private string _SerialLogEnglishFontFamily = "Consolas";
        [Description("串口日志英文字体")]
        public string SerialLogEnglishFontFamily
        {
            get { return _SerialLogEnglishFontFamily; }
            set
            {
                _SerialLogEnglishFontFamily = value;
            }
        }
        /// <summary>
        /// 日志展示控件
        /// </summary>
        public RichTextBox richTextBox
        {
            get
            {
                return this.rtbLog;
            }
        }
        public SerialLog()
        {
            InitializeComponent();
            cmbLogType.SelectedIndex = 0;
            rtbLog.LanguageOption = RichTextBoxLanguageOptions.UIFonts;

            //Framework按钮偏小
            if (!string.IsNullOrEmpty(RuntimeInformation.FrameworkDescription))
            {
                btnClear.Height += 3;
                btnCopy.Height += 3;
                btnCopyLog.Height += 3;
            }
        }


        private void btnCopy_Click(object sender, EventArgs e)
        {
            try
            {
                rtbLog.SelectAll();
                rtbLog.Copy();
                rtbLog.SelectionStart = rtbLog.TextLength;
                MessageBox.Show("复制日志完成", "复制成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "复制失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        Regex regLogCopy = new Regex(@"(.*?\d{2}:\d{2})\sHEX：");
        Regex regLogRemoveText = new Regex(@"TXT：[\s\S]*?\s*\n\n");
        private void btnCopyLog_Click(object sender, EventArgs e)
        {
            try
            {
                string alltext = rtbLog.Text;
                string result = regLogRemoveText.Replace(alltext, "");
                result = regLogCopy.Replace(result, "$1\t");
                Clipboard.SetText(result);
                MessageBox.Show("复制日志完成", "复制成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "复制失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
        }
        public void AddLog(string comName, Color color, byte[] ByteLog)
        {
            if (!_LogEnable) return;
            try
            {

                string hexLog = "";
                if (_SerialLogType == LogType.HEX_And_TEXT || _SerialLogType == LogType.HEX)
                {
                    hexLog=ByteToHex(ByteLog);
                } 
                AddLog(comName, color, ByteLog, hexLog);
            }
            catch (Exception)
            {

            }
        }
        public void AddLog(string comName, Color color, string hexLog)
        {
            if (!_LogEnable) return;
            try
            {
                byte[] bytes = [];
                if (_SerialLogType == LogType.HEX_And_TEXT || _SerialLogType == LogType.TEXT)
                {
                    bytes = HexToByte(hexLog);
                }
                AddLog(comName, color, bytes, hexLog);
            }
            catch (Exception)
            {

            }
        }
        public void AddLog(string comName, Color color, byte[] bytes, string hexLog)
        {
            if (!_LogEnable) return;
            _AddLog("\r\n" + comName + " " + DateTime.Now.ToString("HH:mm:ss"), color, _SerialLogChineseFontFamily);
            if (hexLog.Length == 0&& bytes==null) return;
            StringBuilder sb = new StringBuilder();
            if (_SerialLogType == LogType.HEX_And_TEXT || _SerialLogType == LogType.HEX)
            {
                sb.Append("HEX：");
                string hex = hexLog.Replace(" ", "");
                if (hex.Length % 2 == 0)
                {
                    for (int i = 0; i < hex.Length; i += 2)
                    {
                        if (i != 0)
                        {
                            sb.Append(" ");
                        }
                        sb.Append(hex.Substring(i, 2));
                    }
                }
                else
                {
                    sb.Append(hexLog);
                }
                sb.AppendLine();
            }
            if (_SerialLogType == LogType.HEX_And_TEXT || _SerialLogType == LogType.TEXT)
            {
                try
                {
                    string temp = Encoding.Default.GetString(bytes);
                    temp = temp.Replace("\0", ""); //\0会导致后面的字符不显示
                    sb.AppendLine("TXT：" + temp);
                }
                catch (global::System.Exception)
                {
                }

            }
            _AddLog(sb.ToString(), color, _SerialLogEnglishFontFamily);
        }
        void _AddLog(string content, Color color, string font)
        {
            this.Invoke((MethodInvoker)delegate ()
            {
                rtbLog.SelectionStart = rtbLog.Text.Length;
                rtbLog.SelectionColor = color;
                rtbLog.SelectionFont = new Font(font, rtbLog.Font.Size);
                rtbLog.AppendText(content);
                rtbLog.AppendText("\n");
                Application.DoEvents();
                if (LogAutoScroll)
                {
                    rtbLog.ScrollToCaret();
                }
            });
        }


        /// <summary>
        /// Hex转换成Ascii
        /// 将ASC码转换成字符串，如："414243"转换为"ABC"
        /// </summary>
        /// <param name="hex"></param>
        /// <returns>Ascii信息</returns>
        /// <exception cref="Exception"></exception>
        public static string HexToAscii(string hex)
        {
            string strCharacter = "";
            String str = "";
            hex = hex.Replace(" ", "");
            int j = hex.Length;

            for (int i = 0; i < j - 1; i += 2)
            {
                int asciiCode1 = Convert.ToInt32(hex.Substring(i, 2), 16);

                if (asciiCode1 >= 0x00 && asciiCode1 <= 0xFF)
                {
                    ASCIIEncoding asciiEncoding = new ASCIIEncoding();
                    byte[] byteArray = new byte[] { (byte)asciiCode1 };
                    strCharacter = asciiEncoding.GetString(byteArray);

                }
                else
                {
                    throw new Exception("ASCII Code is not valid.");
                }
                str += strCharacter;
            }
            return str;
        }
        /// <summary>
        /// Ascii转Hex
        /// 将字符串转换成ASC码，如："ABC"转换为"414243"
        /// </summary>
        /// <param name="asciiCode">Ascii代码</param>
        /// <returns>返回的hex信息</returns>
        public static string AsciiToHex(string asciiCode)
        {

            byte[] ba = System.Text.ASCIIEncoding.Default.GetBytes(asciiCode);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in ba)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();

        }

        /// <summary>
        /// 转换十六进制字符串到字节数组："41 42 43"--{0x41,0x42,0x43}
        /// </summary>
        /// <param name="msg">待转换字符串</param>
        /// <returns>字节数组</returns>
        public static byte[] HexToByte(string msg)
        {
            msg = msg.Replace(" ", "");//移除空格
            byte[] comBuffer = new byte[msg.Length / 2];
            for (int i = 0; i < msg.Length; i += 2)
            {
                comBuffer[i / 2] = (byte)Convert.ToByte(msg.Substring(i, 2), 16);
            }
            return comBuffer;
        }

        /// <summary>
        /// 转换字节数组到十六进制字符串:{0x41,0x42,0x43}--"414243"
        /// </summary>
        /// <param name="comByte">待转换字节数组</param>
        /// <returns>十六进制字符串</returns>
        public static string ByteToHex(byte[] comByte)
        {
            string returnStr = "";
            if (comByte != null)
            {
                for (int i = 0; i < comByte.Length; i++)
                {
                    returnStr += comByte[i].ToString("X2");
                }
            }
            return returnStr;
        }

        private void chkAutoScroll_CheckedChanged(object sender, EventArgs e)
        {
            _LogAutoScroll = chkAutoScroll.Checked;
        }

        private void cmbLogType_SelectedIndexChanged(object sender, EventArgs e)
        {
            _SerialLogType = (LogType)cmbLogType.SelectedIndex;
        }

        private void chkEnable_CheckedChanged(object sender, EventArgs e)
        {
            _LogEnable = chkEnable.Checked; ;
        }
    }
}
