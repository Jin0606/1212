using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace Chat_Client
{
    public partial class ChatForm : Form
    {
        //스레드가 동작되고 있는 함수에서는 TextBox에  출력하기 위해 Delegate 를 사용해야한다.
        delegate void AppendTextDelegate(Control ctrl, string s);
        AppendTextDelegate textAppender;
        //소켓생성
        Socket ClientSock;

        public ChatForm()
        {
            InitializeComponent();
            ClientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            textAppender = new AppendTextDelegate(AppendText);
            this.InputTxt.KeyDown += InputTxt_KeyDown;

        }
        //메시지 전송
        void AppendText(Control ctrl, string s)
        {
            // 텍스트를 추가해주는 대리자가 null이면 개체를 생성한다.
            if (textAppender == null) textAppender = new AppendTextDelegate(AppendText);

            // 컨트롤이 생성된 UI Thread가 아니라면 InvokeRequired 속성의 값이 true로 설정된다.
            // 따라서, Invoke를 통한 대리자 호출로 AppendText 메서드가 UI Thread에서 실행되도록 해줘야 한다.
            if (ctrl.InvokeRequired) ctrl.Invoke(textAppender, ctrl, s);
            else
            {
                string source = ctrl.Text;
                ctrl.Text = source + Environment.NewLine + s;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());

            // 처음으로 발견되는 ipv4 주소를 사용한다.
            IPAddress defaultHostAddress = null;
            foreach (IPAddress addr in he.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    defaultHostAddress = addr;
                    break;
                }
            }
            // 주소가 없다면 로컬호스트 주소를 사용한다.
            if (defaultHostAddress == null)
                defaultHostAddress = IPAddress.Loopback;
            txtAddress.Text = defaultHostAddress.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (ClientSock.Connected)
            {
                ConnectMsgBox.Error("이미 연결되어 있습니다!");
                return;
            }
            int port;
            if (!int.TryParse(txtPort.Text, out port))
            {
                ConnectMsgBox.Error("포트 번호가 잘못 입력되었거나 입력되지 않았습니다.");
                txtPort.Focus();
                txtPort.SelectAll();
                return;
            }

            try { ClientSock.Connect(txtAddress.Text, port); }
            catch (Exception ex)
            {
                ConnectMsgBox.Error("연결에 실패했습니다!\n오류 내용: {0}", MessageBoxButtons.OK, ex.Message);
                return;
            }

            // 연결 완료되었다는 메세지를 띄워준다.
            AppendText(txtHistory, "서버와 연결되었습니다.");

            // 연결 완료, 서버에서 데이터가 올 수 있으므로 수신 대기한다.
            AsyncObject obj = new AsyncObject(4096);
            obj.WorkingSocket = ClientSock;
            ClientSock.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
        }
        public class AsyncObject
        {
            // 비동기 작업에서 사용하는 소켓과 해당 작업에 대한 데이터 버퍼를 저장하는 클래스
            public byte[] Buffer;
            public Socket WorkingSocket;
            public readonly int BufferSize;
            public AsyncObject(int bufferSize)
            {
                BufferSize = bufferSize;
                Buffer = new byte[BufferSize];
            }

            public void ClearBuffer()
            {
                Array.Clear(Buffer, 0, BufferSize);
            }
        }
        void DataReceived(IAsyncResult ar)
        {
            // BeginReceive에서 추가적으로 넘어온 데이터를 AsyncObject 형식으로 변환한다.
            AsyncObject obj = (AsyncObject)ar.AsyncState;

            // 데이터 수신을 끝낸다.
            int received = obj.WorkingSocket.EndReceive(ar);
            AppendText(txtHistory, string.Format(received+"asdf"));

            // 받은 데이터가 없으면(연결끊어짐) 끝낸다.
            if (received <= 0)
            {
                obj.WorkingSocket.Close();
                return;
            }

            // 텍스트로 변환한다.
            string text = Encoding.UTF8.GetString(obj.Buffer);

            // 0x01 기준으로 짜른다.
            // tokens[0] - 보낸 사람 IP
            // tokens[1] - 보낸 메세지
            string[] tokens = text.Split('\x01');
            string ip = tokens[0];
            string msg = tokens[1];

            // 텍스트박스에 추가해준다.
            // 비동기식으로 작업하기 때문에 폼의 UI 스레드에서 작업을 해줘야 한다.
            // 따라서 대리자를 통해 처리한다.
            AppendText(txtHistory, string.Format("[받음]{0}: {1}", ip, msg));

            // 클라이언트에선 데이터를 전달해줄 필요가 없으므로 바로 수신 대기한다.
            // 데이터를 받은 후엔 다시 버퍼를 비워주고 같은 방법으로 수신을 대기한다.
            obj.ClearBuffer();

            // 수신 대기
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            // 서버가 대기중인지 확인한다.
            if (!ClientSock.IsBound)
            {
                ConnectMsgBox.Warn("서버가 실행되고 있지 않습니다!");
                return;
            }

            // 보낼 텍스트
            string tts = InputTxt.Text.Trim();
            if (string.IsNullOrEmpty(tts))
            {
                ConnectMsgBox.Warn("텍스트가 입력되지 않았습니다!");
                InputTxt.Focus();
                return;
            }
            // 서버 ip 주소와 메세지를 담도록 만든다.
            IPEndPoint ip = (IPEndPoint)ClientSock.LocalEndPoint;
            string addr = ip.Address.ToString();

            // 문자열을 utf8 형식의 바이트로 변환한다.
            byte[] bDts = Encoding.UTF8.GetBytes(addr + '\x01' + tts);

            // 서버에 전송한다.
            ClientSock.Send(bDts);

            // 전송 완료 후 텍스트박스에 추가하고, 원래의 내용은 지운다.
            AppendText(txtHistory, string.Format("[보냄]{0}: {1}", addr, tts));
            InputTxt.Clear();
        }

        private void InputTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) // 엔터키 눌렀을 때
                if (e.KeyCode == Keys.Enter)
                {
                    //BtnSend.Focus();
                    BtnSend_Click(sender, e);
                }
        }       
        
    }
}
