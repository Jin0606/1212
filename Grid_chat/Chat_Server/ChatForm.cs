using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MultiChatServer {
    public partial class ChatForm : Form {
        delegate void AppendTextDelegate(Control ctrl, string s);
        AppendTextDelegate _textAppender = null;
        Socket ServerSock=null;
        IPAddress thisAddress;
        Socket client=null;
        static int counter = 0;
        bool changeA = false;
        public ChatForm() {
            InitializeComponent();
            ServerSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _textAppender = new AppendTextDelegate(AppendText);
           // FormClosing += new FormClosingEventHandler(MainForm_FormClosing);
        }
  /*      public void MainForm_FormClosing(object sender, FormClosedEventArgs e)
        {
            if (listener != null)
            {
                listener.Close();
                listener.Dispose();

                Application.Exit();
        }*/
        void AppendText(Control ctrl, string s) {
            if (ctrl.InvokeRequired) ctrl.Invoke(_textAppender, ctrl, s);
            else {
                string source = ctrl.Text;
                ctrl.Text = source + Environment.NewLine + s;
            }
        }

        void OnFormLoaded(object sender, EventArgs e) {



            IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());

            // 처음으로 발견되는 ipv4 주소를 사용한다.
            foreach (IPAddress addr in he.AddressList) {
                if (addr.AddressFamily == AddressFamily.InterNetwork) {
                    thisAddress = addr;
                    break;
                }    
            }

            // 주소가 없다면..
            if (thisAddress == null)
            // 로컬호스트 주소를 사용한다.
            thisAddress = IPAddress.Loopback;
            /*thisAddress = "192.168.100.112";
            thisAddress.ToString("192.168.100.112");*/

            // 서버에서 클라이언트의 연결 요청을 대기하기 위해
            // 소켓을 열어둔다.
            IPEndPoint serverEP = new IPEndPoint(thisAddress, 9090);
            ServerSock.Bind(serverEP);
            ServerSock.Listen(10);

            // 비동기적으로 클라이언트의 연결 요청을 받는다.
            ServerSock.BeginAccept(AcceptCallback, null);
        }

        List<Socket> connectedClients = new List<Socket>();
        public Dictionary<Socket, string> clientList = new Dictionary<Socket, string>();
        void AcceptCallback(IAsyncResult ar) {
            
            // 클라이언트의 연결 요청을 수락한다.
            //Socket client = ServerSock.EndAccept(ar);
            client = ServerSock.EndAccept(ar);
            // 또 다른 클라이언트의 연결을 대기한다.
            ServerSock.BeginAccept(AcceptCallback, null);

            //4096바이트의 크기를 갖는 바이트 배열을 가진 AsyncObject 클래스 생성
            AsyncObject obj = new AsyncObject(4096);
            //작업중인 소켓을 저장하기 위해 client 할당
            obj.WorkingSocket = client;
            // 연결된 클라이언트 리스트에 추가해준다.
            connectedClients.Add(client);

            counter++;
            string user_name = "사용자" + counter;

            // 텍스트박스에 클라이언트가 연결되었다고 써준다.
            //AppendText(txtHistory, string.Format("클라이언트 (@ {0})가 연결되었습니다.", client.RemoteEndPoint));
            AppendText(txtHistory, string.Format(user_name+"(@ {0}) 연결되었습니다.", client.RemoteEndPoint));

            // 비동기적으로 들어오는 자료를 수신하기 위해 BeginReceive 사용 , 클라이언트의 데이터를 받는다.
            client.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
        }

        void DataReceived(IAsyncResult ar) {
            // BeginReceive에서 추가적으로 넘어온 데이터를 AsyncObject 형식으로 변환한다.
            AsyncObject obj = (AsyncObject)ar.AsyncState;
            
            // 데이터 수신을 끝낸다.
            int received = obj.WorkingSocket.EndReceive(ar);

            // 받은 데이터가 없으면(연결끊어짐) 끝낸다.
            if (received <= 0) {
                obj.WorkingSocket.Close();
                return;
            }

            // UTF8 인코더를 사용하여 바이트 배열을 문자열로 변환한다.
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
            AppendText(txtHistory, string.Format("[받음]사용자"+counter+": {1}", ip, msg));
           
            if (msg.Contains("연필") && msg.Contains("수량"))
            {
                msg = "현재 연필 개수는 3개 입니다.";
                /*SendMsg(msg);*/
            }
            else if(msg.Contains("대문자로") && msg.Contains("변환"))
            {
                changeA = true;
                msg = "대문자로 변경되었습니다.";
                /*SendMsg(msg);*/
            }
            else if (msg.Contains("소문자로") && msg.Contains("변환"))
            {
                changeA = false;
                msg = "소문자로 변경되었습니다.";
                /*SendMsg(msg);*/
            }
            else if (msg.Contains("현재") && msg.Contains("개") && msg.Contains("이하") && msg.Contains("제품") || msg.Contains("개") && msg.Contains("이하") && msg.Contains("제품"))
            {
                string strTmp = Regex.Replace(msg,@"\D","");
                int nTmp = int.Parse(strTmp);
                msg = "현재 " + nTmp + "개 이하인 제품은 지우개, 볼펜 입니다.";
                /*SendMsg(msg);*/
            }
            else if (msg.Contains("현재") && msg.Contains("개") && msg.Contains("이하") && msg.Contains("제품") || msg.Contains("개") && msg.Contains("이상") && msg.Contains("제품"))
            {
                string strTmp = Regex.Replace(msg, @"\D", "");
                int nTmp = int.Parse(strTmp);
                msg = "현재 " + nTmp + "개 이상인 제품은 노트, 형광펜 입니다.";
                /*SendMsg(msg);*/
            }
            else if (msg.Contains("현재") && msg.Contains("상태"))
            {
                List < string > mm= new List<string>();
                mm.Add("현재 가동중입니다.");
                mm.Add("현재 사용 중지상태 입니다.");
                mm.Add("현재 수리중입니다.");
                Random rd = new Random();
                msg = mm[rd.Next(0,3)];
                /*SendMsg(msg);*/
            }
            else if (msg.Contains("완제품") && msg.Contains("재고현황") || msg.Contains("분석"))
            {
                
                msg = SendDate()+" 기준 완제품 재고현황을 분석합니다.\n 연필 3개, 노트 5개, 형광펜 7개 입니다.";
                /*SendMsg(msg);*/
            }
            else if (msg.Contains("직원") && msg.Contains("주소") && msg.Contains("수"))
            {
               string addrP = UserAddress(msg);
                msg = "주소가 " + addrP + "인 직원은 5명 입니다.";
                /*SendMsg(msg);*/
            }
            else if (msg.Contains("메모리") && msg.Contains("정리"))
            {
                msg = "메모리를 정리했습니다.";
                /*SendMsg(msg);*/
            }
            else if (msg.Contains("오늘") && msg.Contains("날씨") || msg.Contains("오늘") && msg.Contains("기온"))
            {
                msg = "오늘의 날씨 :  최저 온도 0도, 최고 온도 10도, 흐림";
               /* SendMsg(msg);*/
            }
            if (changeA)
            {
                msg.ToUpper();
            }
            // 데이터를 받은 후엔 다시 버퍼를 비워주고 같은 방법으로 수신을 대기한다.
            obj.ClearBuffer();

            // 수신 대기
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
            //서버 ip주소와 메시지 담기
            IPEndPoint clientIp = (IPEndPoint)ServerSock.LocalEndPoint;
            string addr = clientIp.Address.ToString();

            // 문자열을 utf8 형식의 바이트로 변환한다.
            byte[] bDts = Encoding.UTF8.GetBytes(thisAddress.ToString() + '\x01' + msg);

            for (int i = connectedClients.Count - 1; i >= 0; i--)
            {
                if (connectedClients[i] == client)
                {
                    //요청한 클라이언트에 전송
                    Socket socket = connectedClients[i];
                    try { socket.Send(bDts);
                    }
                    catch
                    {
                        // 오류 발생하면 전송 취소하고 리스트에서 삭제한다.
                        try { socket.Dispose(); } catch { }
                        connectedClients.RemoveAt(i);
                    }
                }else{
                    //모든 클라이언트에 전송
                    Socket socket = connectedClients[i];
                    try { socket.Send(bDts); }
                    catch
                    {
                        // 오류 발생하면 전송 취소하고 리스트에서 삭제한다.
                        try { socket.Dispose(); } catch { }
                        connectedClients.RemoveAt(i);
                    }
                }
                
            }
            // 데이터를 받은 후엔 다시 버퍼를 비워주고 같은 방법으로 수신을 대기한다.
            obj.ClearBuffer();
            // 수신 대기
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
        }
        private void SendMsg(string msg)
        {
            if (changeA)
            {
                msg.ToUpper();
            }
            //서버 ip주소와 메시지 담기
            IPEndPoint clientIp = (IPEndPoint)ServerSock.LocalEndPoint;
            string addr = clientIp.Address.ToString();

            // 문자열을 utf8 형식의 바이트로 변환한다.
            byte[] bDts = Encoding.UTF8.GetBytes(thisAddress.ToString() + '\x01' + msg);

            for (int i = connectedClients.Count - 1; i >= 0; i--)
            {
                if (connectedClients[i] == client)
                {
                    //요청한 클라이언트에 전송
                    Socket socket = connectedClients[i];
                    try
                    {
                        socket.Send(bDts);
                    }
                    catch
                    {
                        // 오류 발생하면 전송 취소하고 리스트에서 삭제한다.
                        try { socket.Dispose(); } catch { }
                        connectedClients.RemoveAt(i);
                    }
                }
                else
                {
                    //모든 클라이언트에 전송
                    Socket socket = connectedClients[i];
                    try { socket.Send(bDts); }
                    catch
                    {
                        // 오류 발생하면 전송 취소하고 리스트에서 삭제한다.
                        try { socket.Dispose(); } catch { }
                        connectedClients.RemoveAt(i);
                    }
                }

            }


        }

        //주소
        string UserAddress(string msg)
        {
            string addrP = "";

            if (msg.Contains("서울"))
            {
                addrP = "서울";
            }
            else if (msg.Contains("경기도"))
            {
                addrP = "경기도";
            }
            else if (msg.Contains("충청남도") || msg.Contains("충남"))
            {
                addrP = "충청남도";
            }
            else if (msg.Contains("충청북도") || msg.Contains("충북"))
            {
                addrP = "충청북도";
            }
            else if (msg.Contains("제주도"))
            {
                addrP = "제주도";
            }
            else if (msg.Contains("전라남도") || msg.Contains("전남"))
            {
                addrP = "전라남도";
            }
            else if (msg.Contains("전라북도") || msg.Contains("전북"))
            {
                addrP = "전라북도";
            }
            else if (msg.Contains("경상남도") || msg.Contains("경남"))
            {
                addrP = "경상남도";
            }
            else if (msg.Contains("경상북도") || msg.Contains("경북"))
            {
                addrP = "경상북도";
            }
            return addrP;
        }
        //날짜
        public string SendDate()
        {
            //현재날짜시간
            string date = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss"); 
            return date;
        }
    }
}