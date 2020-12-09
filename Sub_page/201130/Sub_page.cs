using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;


namespace Sub_page
{
    public partial class Sub_page : Form
    {
        SqlConnection con;
        SqlCommand cmd;
        SqlDataReader ars;

        public Sub_page()
        {
            InitializeComponent();
            //DateTimePicker format 초기 셋팅
            tb_pdDate.CustomFormat = "yyyy-MM-dd";
            tb_pdDate.Format = DateTimePickerFormat.Custom;

            //DB연결
            con = new SqlConnection();
            cmd = new SqlCommand();
            con.ConnectionString= "Server=localhost;DataBase=tempdb;uid=vsUserTest;pwd=userTest;Persist Security Info=true";
            con.Open();
            cmd.Connection = con;
            // 행 전체 선택
            GridViewProduct.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Btn_select_Click(null,null);
        }
      
        //등록
        private void Btn_create_Click(object sender, EventArgs e)
        {
            //Dbconnection();
            if (Tb_pdNum.Text == "" || Tb_pdName.Text == "" || Tb_pdOderKg.Text== "" || Tb_unitPrice.Text== "" || 
                Tb_buyer.Text =="" || Tb_PackagingKg.Text== "" || Tb_pdSum.Text== "")
            {
                MessageBox.Show("입력하지 않은 칸이 있습니다");
                return;
            }else
            {
                //예외처리
                try
                {
                    string strSql = "insert into sampleTb values(NEXT value for sampleTb_seq,'" + Tb_pdNum.Text + "','" + Tb_pdName.Text + "'," + Tb_pdOderKg.Text + ","
                                    + Tb_PackagingKg.Text + "," + Tb_unitPrice.Text + "," + Tb_pdSum.Text + ",'" + tb_pdDate.Text + "')";
                    cmd = new SqlCommand(strSql, con);
                    if (cmd.ExecuteNonQuery() == 1)
                    {
                        MessageBox.Show("등록되었습니다.");
                        prevText = string.Empty;
                        Tb_pdNum.Text ="";
                        Tb_pdName.Text = "";
                        Tb_pdOderKg.Text = "";
                        Tb_buyer.Text = "";
                        Tb_unitPrice.Text = "";
                        Tb_PackagingKg.Text = "";
                        Tb_pdSum.Text = "";                     
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("등록에 실패했습니다.");
                }
            }
            Btn_select_Click(null, null);
        }

        //조회
        private void Btn_select_Click(object sender, EventArgs e)
        {
            cmd.CommandText = "select *from sampleTb";
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            GridViewProduct.DataSource = dt.DefaultView;
            
            //MessageBox.Show("dt: " + dt);
            /*string strSql = "select *from sampleTb";
            cmd = new SqlCommand(strSql, con);
            ars = cmd.ExecuteReader();

            //Row index
            int a = 0;
            //GridView 초기화
            GridViewProduct.Rows.Clear();

            //ars에 있으면 진행, 없으면 건너뜀
            while (ars.Read())
            {
                //Row 한 칸 늘림
                GridViewProduct.Rows.Add();

                //ars.FieldCount ars에 들어있는 필드의 개수
                for (int i = 0; i < ars.FieldCount; i++)
                {
                    if (i == 6)
                    {
                        GridViewProduct.Rows[a].Cells[i + 1].Value = string.Format("{0:yyyy/MM/dd}", ars[i]);
                    }
                    else
                    {
                        //DB에서 select해 온 데이터 그리드에 하나씩 뿌리기
                        GridViewProduct.Rows[a].Cells[i + 1].Value = ars[i].ToString();
                    }
                }
                //Row 카운드 증가
                a++;
            }
            ars.Close();*/

        }
        //수정
        private void Btn_update_Click(object sender, EventArgs e)
        {
            DataTable dtChanges;
            DataView dvScore = (DataView)GridViewProduct.DataSource;
            dtChanges = dvScore.Table.GetChanges(DataRowState.Modified);

            if (dtChanges != null)
            {
                if (MessageBox.Show("데이터를 수정하시겠습니까?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    string strSql;
                    for (int i = 0; i < dtChanges.Rows.Count; i++)
                    {
                        strSql = "update sampleTb set ";
                        if (dtChanges.Rows[i]["pdId"].ToString() != null)
                        {
                            strSql += "pdId='" + dtChanges.Rows[i]["pdId"].ToString() + "',";
                        }
                        if (dtChanges.Rows[i]["pdName"].ToString() != null)
                        {
                            strSql += "pdName='" + dtChanges.Rows[i]["pdName"].ToString() + "',";
                        }
                        if (dtChanges.Rows[i]["orderNum"].ToString() != null)
                        {
                            strSql += "orderNum='" + dtChanges.Rows[i]["orderNum"].ToString() + "',";
                        }
                        if (dtChanges.Rows[i]["packagingNum"].ToString() != null)
                        {
                            strSql += "packagingNum='" + dtChanges.Rows[i]["packagingNum"].ToString() + "',";
                        }
                        if (dtChanges.Rows[i]["unitPrice"].ToString() != null)
                        {
                            strSql += "unitPrice='" + dtChanges.Rows[i]["unitPrice"].ToString() + "',";
                        }
                        if (dtChanges.Rows[i]["sumNum"].ToString() != null)
                        {
                            strSql += "sumNum='" + dtChanges.Rows[i]["sumNum"].ToString() + "'";
                        }
                        strSql += " From sampleTb where idx='" + dtChanges.Rows[i]["idx"].ToString() + "'";
                        cmd.CommandText = strSql;
                        cmd.ExecuteNonQuery();

                    }
                }
            }
            else
            {
                MessageBox.Show("수정할 데이터가 없습니다.");
                return;
            }
            Btn_select_Click(null, null);
        
        }
        //삭제
        private void Btn_delete_Click(object sender, EventArgs e)
        {
            if (GridViewProduct.Rows.Count < 1)
            {
                //행이 없을때
                MessageBox.Show("삭제할 항목이 없습니다.", "삭제에러", MessageBoxButtons.OK);
                return;
            }
            else
            {
                if (MessageBox.Show("선택된 데이터를 삭제하시겠습니까?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    for (int i = 0; i < GridViewProduct.Rows.Count; i++)
                    {
                        //gridview 전체에서 체크박스 값만
                        DataGridViewCell cell = GridViewProduct.Rows[i].Cells[0];
                        //체크 유무확인                 
                        bool isChecked = (bool)cell.EditedFormattedValue;
                        if (isChecked)
                        {
                            string idx = GridViewProduct.Rows[i].Cells[1].FormattedValue.ToString();
                            string strSql = "delete from sampleTb where idx =" + idx;
                            cmd.CommandText = strSql;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }              
            }
            Btn_select_Click(null, null);
        }

        //textBox에 입력한 값을 저장 //데이터 입력후 등록시 비워줘야함
        string prevText = string.Empty;

        private void Tb_pdOderKg_TextChanged(object sender, EventArgs e)
        {
            //숫자만 입력받게 함
            double value = 0;
            if (double.TryParse(this.Tb_pdOderKg.Text, out value) == false)
            {
                //변환할 수 없으면 이전 텍스트 값으로 재설정
                this.Tb_pdOderKg.Text = prevText;
            }
            else
            {
                prevText = this.Tb_pdOderKg.Text;
            }
        }


        
        private void Tb_unitPrice_TextChanged(object sender, EventArgs e)
        {
            //숫자만 입력받게 함
 
            double value = 0;
            if (double.TryParse(this.Tb_unitPrice.Text, out value) == false)
            {
                //변환할 수 없으면 이전 텍스트 값으로 재설정
                this.Tb_unitPrice.Text = prevText;
            }
            else
            {
                prevText = this.Tb_unitPrice.Text;
            }
        }

        private void Tb_PackagingKg_TextChanged(object sender, EventArgs e)
        {
            //숫자만 입력받게 함
            //string prevText = string.Empty;
            double value = 0;
            if (double.TryParse(this.Tb_PackagingKg.Text, out value) == false)
            {
                //변환할 수 없으면 이전 텍스트 값으로 재설정
                this.Tb_PackagingKg.Text = prevText;
            }
            else
            {
                prevText = this.Tb_PackagingKg.Text;
            }
        }

        private void Tb_pdSum_TextChanged(object sender, EventArgs e)
        {
            //숫자만 입력받게 함
            //string prevText = string.Empty;
            double value = 0;
            if (double.TryParse(this.Tb_pdSum.Text,out value) == false)
            {
                //변환할 수 없으면 이전 텍스트 값으로 재설정
                this.Tb_pdSum.Text = prevText;
            }
            else
            {
                prevText = this.Tb_pdSum.Text;
            }
        }
    }
}
