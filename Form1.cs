using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.IO;
using System.Data.OleDb;
using Emgu.CV.CvEnum;
using Excel = Microsoft.Office.Interop.Excel; 

namespace StudentAttendanceManagementSystem
{
    public partial class Form1 : Form
    {
        Capture capture;
        string data = null;

        int ia = 0;

        int ja = 0; 
        Image<Bgr, byte> ImageFrame;
        //EigenObjectRecognizer recognizer;
        FaceRecognizer recognizer;
        private bool captureInProgress;
        int i = 0;
        private HaarCascade haar = null;  //the viola-jones classifier(detector)
        String[] K = { "a", "b", "c", "d" };

        int[] rno;
        Bitmap[] ExtFaces;
        Bitmap[] FaceRecc;
        int faceNo = 0;
        Image<Gray, Byte> myImage1;

        //For database connection
        //OleDb connection
        OleDbConnection connection = new OleDbConnection();
        OleDbConnection markatt = new OleDbConnection();
        //Data adapter
        OleDbDataAdapter DataAdapter;
        //Create data table to hold access values
        DataTable TSTable = new DataTable();

        //variables to navigate
        int TotalRows = 0;
        int RowNumber = 0;

        //Attendance Record
        String AttendYear = null;
        String AttendBranch = null;
        String AttendSem = null;
        String AttendSub = null;
        String AttendDate = null;


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // adjust path to find your XML file 
            haar = new HaarCascade("haarcascade_frontalface_alt2.xml");

            //connect to database

            ConnectToDatabase();



        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            #region if capture is not created, create it now
            if (capture == null)
            {
                try
                {
                    capture = new Capture(0);
                }
                catch (NullReferenceException excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
            }
            #endregion

            if (capture != null)
            {
                if (captureInProgress)
                {  //if camera is getting frames then stop the capture and set button Text
                    // "Start" for resuming capture
                    btnStart.Text = "Start!"; //
                    Application.Idle -= ProcessFrame;
                }
                else
                {
                    //if camera is NOT getting frames then start the capture and set button
                    // Text to "Stop" for pausing capture
                    btnStart.Text = "Stop";
                    Application.Idle += ProcessFrame;
                }

                captureInProgress = !captureInProgress;
            }
            btnCaptureImage.Enabled = true;
            btnDetect.Enabled = true;

        }

        private void ReleaseData()
        {
            if (capture != null)
                capture.Dispose();
        }

        private void ProcessFrame(object sender, EventArgs arg)
        {
            ImageFrame = capture.QueryFrame().Resize(640, 480, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);  //line 1
            InputImageBox.Image = ImageFrame;        //line 2

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Image InputImg = Image.FromFile(openFileDialog1.FileName);
                ImageFrame = new Image<Bgr, byte>(new Bitmap(InputImg));
                InputImageBox.Image = ImageFrame.Resize(640, 480, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC); ;
            }

            btnCaptureImage.Enabled = true;

            btnDetect.Enabled = true;
        }

        private void btnCaptureImage_Click(object sender, EventArgs e)
        {

            ImageFrame.Save(@"E:\mynewpic" + i + ".jpg");
            i++;
        }

        private void btnDetect_Click(object sender, EventArgs e)
        {
            //Change text of start button
            btnStart.Text = "Start"; //
            captureInProgress = false;

            //Pause the live streaming video
            Application.Idle -= ProcessFrame;

            //Call face detection
            DetectFaces();
        }

        private void DetectFaces()
        {
            Image<Gray, byte> grayframe = ImageFrame.Convert<Gray, byte>();


            //detect faces from the gray-scale image and store into an array of type 'var',i.e 'MCvAvgComp[]'
            var faces = grayframe.DetectHaarCascade(
                haar,
                1.2,
                2,
                HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                new Size(25, 25))[0];

            Bitmap BmpInput = grayframe.ToBitmap();
            Bitmap ExtractedFace; //empty
            Graphics FaceCanvas; //empty

            ExtFaces = new Bitmap[faces.Length];
            FaceRecc = new Bitmap[faces.Length];

            //draw a green rectangle on each detected face in image
            foreach (var face in faces)
            {
                ImageFrame.Draw(face.rect, new Bgr(Color.Green), 3);

                //set the size of the empty box(ExtractedFace) for image to store
                ExtractedFace = new Bitmap(face.rect.Width, face.rect.Height);

                //set empty image as face canvas
                FaceCanvas = Graphics.FromImage(ExtractedFace);

                FaceCanvas.DrawImage(BmpInput, 0, 0, face.rect, GraphicsUnit.Pixel);
                try
                {
                    ExtFaces[faceNo] = ExtractedFace;
                    FaceRecc[faceNo] = ExtractedFace;
                    faceNo++;
                }
                catch (System.IndexOutOfRangeException ew)
                {
                    MessageBox.Show("Invalid Input");
                }

            }
            try
            {
                faceNo = 0;
                pbDetectedFaces.Image = ExtFaces[faceNo];

                //Display the detected faces in imagebox
                InputImageBox.Image = ImageFrame;

                Next.Enabled = true;
                Previous.Enabled = true;
                DFName.Enabled = true;
                DFRollNo.Enabled = true;
                btnAddToTrainingSet.Enabled = true;
                btnRecognize.Enabled = true;

            }
            catch (Exception e)
            {
                MessageBox.Show("No faces found. Please provide valid input");
            }


        }

        private void btnAddToTrainingSet_Click(object sender, EventArgs e)
        {
            //Check if name and roll number are blank
            if (DFName.Text == "")
            {
                if (DFRollNo.Text == "")
                {
                    MessageBox.Show("Enter Name and Roll Number.");
                }
                else
                {
                    MessageBox.Show("Enter Name.");
                }
            }
            else if (DFRollNo.Text == "")
            {
                MessageBox.Show("Enter Roll Number.");
            }
            else
                AddFaceToDB(pbDetectedFaces.Image, DFName.Text);

        }

        private void Next_Click(object sender, EventArgs e)
        {
            if (faceNo < ExtFaces.Length - 1)
            {
                faceNo++;
                pbDetectedFaces.Image = ExtFaces[faceNo];
            }
            else
                MessageBox.Show("Last Image");
        }

        private void Previous_Click(object sender, EventArgs e)
        {
            if (faceNo > 0)
            {
                faceNo--;
                pbDetectedFaces.Image = ExtFaces[faceNo];
            }
            else
                MessageBox.Show("First Image");
        }
        
        private void btnRecognize_Click(object sender, EventArgs e)
        {
            rno=new int[10];
            int i = 0;
            Image<Gray, byte>[] imgArray = new Image<Gray, byte>[TotalRows];
            String[] name = new String[TotalRows];
            int[] num=new int[TotalRows];
            for (i = 0; i < TotalRows; i++)
            {
                //binary to byte
                try
                {
                    //binary to byte
                    byte[] FetchedImgbyte = (byte[])TSTable.Rows[i]["FaceImage"];



                    //byte to stream
                    MemoryStream FetchedImgstream = new MemoryStream(FetchedImgbyte);

                    Bitmap bm = new Bitmap(FetchedImgstream);



                    //binary to byte
                    name[i] = TSTable.Rows[i]["FaceName"].ToString();

                    num[i]=i;

                    Image<Gray, byte> Fetch = new Image<Gray, byte>(bm).Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                    imgArray[i] = Fetch;
                }
                catch (Exception ee)
                {
                    MessageBox.Show("dcxzdscx");
                }

            }

            

            //MCvTermCriteria termCrit = new MCvTermCriteria(1, 0.001);

            //Eigen face recognizer

            //recognizer = new EigenObjectRecognizer(

            recognizer= new LBPHFaceRecognizer(1, 8, 8, 8, 90);//50

            recognizer.Train(imgArray.ToArray(), num.ToArray());

            // imgArray.ToArray(),

             //name.ToArray(),

             //5000,
            // ref termCrit);


            for (int n = 0; n < FaceRecc.Length; n++)
            {
                
                Bitmap ss = new Bitmap(FaceRecc[n], 100, 100);

                myImage1 = new Image<Gray, Byte>(ss);
                try
                {
                    FaceRecognizer.PredictionResult er = recognizer.Predict(myImage1);
                    int eer = Convert.ToInt16(er.Label.ToString());
                    String facename=name[eer].ToString();
                    //MessageBox.Show(facename);

                    rno[n] = Convert.ToInt16(TSTable.Rows[eer]["RollNo"]);

                    //MessageBox.Show(rno[n].ToString());

                }
                catch (Exception ww)
                {
                    //MessageBox.Show("Unknown Face");
                }

            }
                btnMarkAttendance.Enabled = true;
            

        }

        //---------------------------------------------------------//
        //-------------Connect to database--------------------------//

        private void ConnectToDatabase()
        {
            //set path for db file
            connection.ConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=FacesDatabase.mdb";

            //open the connection to database
            connection.Open();

            // get data using data adapter
            DataAdapter = new OleDbDataAdapter("Select * from TrainingSet1", connection);

            //command to update database
            OleDbCommandBuilder CommandBuilder = new OleDbCommandBuilder(DataAdapter);

            //fill tstable
            DataAdapter.Fill(TSTable);

            //set total rows so that we can add new image at the last row
            if (TSTable.Rows.Count != 0)
            {
                TotalRows = TSTable.Rows.Count;
            }


        }

        //Refresh DB Connection//

        private void RefreshDBConnection()
        {

            if (connection.State.Equals(ConnectionState.Open))
            {
                connection.Close();
                TSTable.Clear();
                ConnectToDatabase();
            }

        }

        //Get face from Database
        private Image GetFaceFromDB()
        {
            //connection is open
            //read data from db and transform into image
            //binary > bytes > memory stream > image

            Image FetchedImg;
            if (RowNumber >= 0)
            {

                //binary to byte
                byte[] FetchedImgbyte = (byte[])TSTable.Rows[RowNumber]["FaceImage"];

                //byte to stream
                MemoryStream FetchedImgstream = new MemoryStream(FetchedImgbyte);

                //stream to image
                FetchedImg = Image.FromStream(FetchedImgstream);

                //return fetched image
                return FetchedImg;
            }
            else
            {
                MessageBox.Show("Database is empty. Please add some data");
                return null;
            }

        }


        private void AddFaceToDB(Image InputImage, string FaceName)
        {
            if (connection.State.Equals(ConnectionState.Closed))
            {
                connection.Open();
            }
            try
            {
                TotalRows++;
                byte[] FaceAsBytes = ConvertToDBFormat(InputImage);

                int aaa = TSTable.Rows.Count;
                aaa = aaa + 2;
                //insert image at the last location 
                OleDbCommand insert = new OleDbCommand("Insert INTO TrainingSet1 VALUES('" + aaa + "','" + DFRollNo.Text + "','" + FaceName + "',@FaceImg)", connection);
                OleDbParameter imageParameter = insert.Parameters.AddWithValue("@FaceImage", SqlDbType.Binary);
                imageParameter.Value = FaceAsBytes;
                imageParameter.Size = FaceAsBytes.Length;
                int rowsAffected = insert.ExecuteNonQuery();

                //for image parameters
                pbDetectedFaces.Image = null;
                DFName.Text = null;
                DFRollNo.Text = null;
                InputImageBox.Image = null;

            }
            catch (Exception ew)
            {
                MessageBox.Show("Roll number already exists in the database.Please enter some other roll number");
            }
            finally
            {
                RefreshDBConnection();

            }

        }

        //convert to byte
        private byte[] ConvertToDBFormat(Image InputImage)
        {
            //convert picturebox image to bitmap
            Bitmap BmpImage = new Bitmap(InputImage, 100, 100);

            //create empty memory stream
            MemoryStream stream = new MemoryStream();

            //convert BmpImage to memory stream stream
            BmpImage.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);

            //convert bitmap to byte array
            byte[] ImageAsBytes = stream.ToArray();

            return ImageAsBytes;

        }

        private void viewTS_Click(object sender, EventArgs e)
        {
            if (TSTable.Rows.Count == 0)
            {
                MessageBox.Show("There is no data is Training Set");
            }
            else
            {


                //show image in form
                pbTrainingSet.Image = GetFaceFromDB();

                //read face label and display in textbox
                TSName.Text = TSTable.Rows[0]["FaceName"].ToString();

                //show face number in label
                TSRollNo.Text = TSTable.Rows[0]["RollNo"].ToString();

                label11.Text = TSTable.Rows[0]["FaceID"].ToString();

                TSName.Enabled = true;
                btnDelete.Enabled = true;
                btnUpdate.Enabled = true;
                btnFirst.Enabled = true;
                btnLast.Enabled = true;
                btnPrev.Enabled = true;
                btnNext.Enabled = true;
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            //face number label
            TSRollNo.Text = TSTable.Rows[RowNumber]["RollNo"].ToString();

            //update name in tstable
            TSTable.Rows[RowNumber]["FaceName"] = TSName.Text;

            label11.Text = TSTable.Rows[RowNumber]["FaceID"].ToString();

            OleDbCommand update = new OleDbCommand("Update TrainingSet1 set FaceName='" + TSName.Text + "'where RollNo='" + TSRollNo.Text + "';", connection);

            update.ExecuteNonQuery();


            RefreshDBConnection();
            //show message to the user 
            MessageBox.Show("Data updated Successfully.");

        }

        private void btnDelete_Click(object sender, EventArgs e)
        {

            if (RowNumber > 0)
            {

                //delete row from tstable
                OleDbCommand deleteRow = new OleDbCommand("Delete from TrainingSet1 where FaceID='" + label11.Text + "';", connection);

                deleteRow.ExecuteNonQuery();

                //update delete from access
                DataAdapter.Update(TSTable);

                RefreshDBConnection();
                //decrease row number
                RowNumber--;
                pbTrainingSet.Image = GetFaceFromDB();
                TSName.Text = TSTable.Rows[RowNumber]["FaceName"].ToString();
                TSRollNo.Text = TSTable.Rows[RowNumber]["RollNo"].ToString();
                label11.Text = TSTable.Rows[RowNumber]["FaceID"].ToString();
                MessageBox.Show("Face deleted successfully.");
                
            }
            else if (RowNumber == 0)
            {
                if (TSTable.Rows.Count == 1)
                {
                    //delete row from tstable
                    OleDbCommand deleteRow = new OleDbCommand("Delete from TrainingSet1 where FaceName='" + TSName.Text + "';", connection);

                    deleteRow.ExecuteNonQuery();

                    DataAdapter.Update(TSTable);

                    pbTrainingSet.Image = null;
                    TSName.Text = "";
                    TSRollNo.Text = "";
                    label11.Text = "";

                    MessageBox.Show("Face deleted successfully.");

                    //decrease row number
                    RowNumber--;
                    TSName.Enabled = false;
                    TSRollNo.Enabled = false;
                    btnDelete.Enabled = false;
                    btnUpdate.Enabled = false;
                    btnFirst.Enabled = false;
                    btnLast.Enabled = false;
                    btnPrev.Enabled = false;
                    btnNext.Enabled = false;
                    RefreshDBConnection();
                }
                else
                {
                    //delete row from tstable
                    OleDbCommand deleteRow = new OleDbCommand("Delete from TrainingSet1 where FaceName='" + TSName.Text + "';", connection);

                    deleteRow.ExecuteNonQuery();
                    try
                    {
                        //update delete from access
                        DataAdapter.Update(TSTable);
                    }
                    catch (Exception ee)
                    {
                        MessageBox.Show(ee.Message.ToString());
                    }

                    pbTrainingSet.Image = GetFaceFromDB();
                    TSName.Text = TSTable.Rows[RowNumber + 1]["FaceName"].ToString();
                    TSRollNo.Text = TSTable.Rows[RowNumber + 1]["RollNo"].ToString();
                    MessageBox.Show("Face deleted successfully.");
                    //decrease row number
                    RowNumber--;
                    RefreshDBConnection();
                }
            }
        }

        private void TSName_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnFirst_Click(object sender, EventArgs e)
        {
            try
            {
                //first refresh the database connection
                RefreshDBConnection();

                //set row number
                RowNumber = 0;

                //show image in form
                pbTrainingSet.Image = GetFaceFromDB();

                //read face label and display in textbox
                TSName.Text = TSTable.Rows[RowNumber]["FaceName"].ToString();

                //show face number in label
                TSRollNo.Text = TSTable.Rows[RowNumber]["RollNo"].ToString();

                label11.Text = TSTable.Rows[RowNumber]["FaceID"].ToString();
           

                MessageBox.Show("You have reached first row of the database");
            }
            catch (Exception qq)
            {
                MessageBox.Show(qq.Message.ToString());
            }
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            //if first face is not reached
            if (RowNumber > 0)
            {
                RowNumber--;
                pbTrainingSet.Image = GetFaceFromDB();
                TSName.Text = TSTable.Rows[RowNumber]["FaceName"].ToString();
                TSRollNo.Text = TSTable.Rows[RowNumber]["RollNo"].ToString();
                label11.Text = TSTable.Rows[RowNumber]["FaceID"].ToString();
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            //if first face is not reached
            if (RowNumber < TotalRows - 1)
            {
                RowNumber++;
                pbTrainingSet.Image = GetFaceFromDB();
                TSName.Text = TSTable.Rows[RowNumber]["FaceName"].ToString();
                TSRollNo.Text = TSTable.Rows[RowNumber]["RollNo"].ToString();
                label11.Text = TSTable.Rows[RowNumber]["FaceID"].ToString();
           
            }
        }

        private void btnLast_Click(object sender, EventArgs e)
        {

            try
            {
                //first refresh the database connection
                RefreshDBConnection();

                RowNumber = TotalRows - 1;

                //show image in form
                pbTrainingSet.Image = GetFaceFromDB();

                //read face label and display in textbox
                TSName.Text = TSTable.Rows[RowNumber]["FaceName"].ToString();

                //show face number in label
                TSRollNo.Text = TSTable.Rows[RowNumber]["RollNo"].ToString();

                label11.Text = TSTable.Rows[RowNumber]["FaceID"].ToString();
           

                MessageBox.Show("You have reached last row of the database");
            }
            catch (Exception qq)
            {
                MessageBox.Show(qq.Message.ToString());
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            connection.Close();
            System.Windows.Forms.Application.Exit();
        }

        private void btnMarkAttendance_Click(object sender, EventArgs e)
        {
            cbBranch.Enabled = true;
            cbSem.Enabled = true;
            cbSub.Enabled = true;
            cbYear.Enabled = true;
            dtp.Enabled = true;
            btnMark.Enabled = true;
            btnEdit.Enabled = true;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbBranch.Text == "IT" || cbBranch.Text == "EXTC" || cbBranch.Text == "CMPN" || cbBranch.Text == "ETRX" || cbBranch.Text == "IT")
            {
                cbSem.Enabled = true;
                cbSem.Text = null;
                cbSem.Items.Clear();
                if (cbYear.Text == "FE")
                {
                    cbSem.Items.Add("sem 1");
                    cbSem.Items.Add("sem 2");
                }
                else if (cbYear.Text == "SE")
                {
                    cbSem.Items.Add("sem 3");
                    cbSem.Items.Add("sem 4");
                }
                else if (cbYear.Text == "TE")
                {
                    cbSem.Items.Add("sem 5");
                    cbSem.Items.Add("sem 6");
                }
                else if (cbYear.Text == "BE")
                {
                    cbSem.Items.Add("sem 7");
                    cbSem.Items.Add("sem 8");
                }
            }
            else
            {
                MessageBox.Show("Select Branch from given options.");
            }

        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbYear.Text == "FE" || cbYear.Text == "SE" || cbYear.Text == "TE" || cbYear.Text == "BE")
            {

                if (cbSem.Text == "sem 1")
                {
                    cbSem.Items.Add("sem 1");
                    cbSem.Items.Add("sem 2");
                }
                else if (cbYear.Text == "SE")
                {
                    cbSem.Items.Add("sem 3");
                    cbSem.Items.Add("sem 4");
                }
                else if (cbYear.Text == "TE")
                {
                    cbSem.Items.Add("sem 5");
                    cbSem.Items.Add("sem 6");
                }
                else if (cbYear.Text == "BE")
                {
                    cbSem.Items.Add("sem 7");
                    cbSem.Items.Add("sem 8");
                }
            }
            else
            {
                MessageBox.Show("Select Semester from given options.");
            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbSem.Text == "sem 1" || cbSem.Text == "sem 2" || cbSem.Text == "sem 3" || cbSem.Text == "sem 4" || cbSem.Text == "sem 5" || cbSem.Text == "sem 6" || cbSem.Text == "sem 7" || cbSem.Text == "sem 8")
            {
                dtp.Enabled = true;
                dtp.Text = null;
                if (cbSem.Text == "sem 1")
                {
                    cbSem.Items.Add("sem 1");
                    cbSem.Items.Add("sem 2");
                }
                else if (cbYear.Text == "SE")
                {
                    cbSem.Items.Add("sem 3");
                    cbSem.Items.Add("sem 4");
                }
                else if (cbYear.Text == "TE")
                {
                    cbSem.Items.Add("sem 5");
                    cbSem.Items.Add("sem 6");
                }
                else if (cbYear.Text == "BE")
                {
                    cbSem.Items.Add("sem 7");
                    cbSem.Items.Add("sem 8");
                }
            }
            else
            {
                MessageBox.Show("Select Semester from given options.");
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void btnMark_Click(object sender, EventArgs e)
        {
            AttendDate = dtp.Text;
            AttendBranch = cbBranch.Text;
            AttendYear = cbYear.Text;
            AttendSem = cbSem.Text;
            AttendSub = cbSub.Text;
            //set path for db file
            markatt.ConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=Attendance\"+ cbBranch.Text +@"\"+cbSem.Text+@"\Attendance.mdb";

            //open the connection to database
            markatt.Open();
            OleDbCommand qq = new OleDbCommand("ALTER TABLE MarkAttendance ADD COLUMN '" + dtp.Text + "' Text", markatt);
            qq.ExecuteNonQuery();

            String p = "P";
            String a = "A";
            for (int l = 0; l < rno.Length; l++)
            {
                OleDbCommand markAttedance = new OleDbCommand("UPDATE MarkAttendance SET '" + dtp.Text + "'= '" + a + "';", markatt);
                markAttedance.ExecuteNonQuery();
                RefreshDBConnection();
            }
            for (int l = 0; l < rno.Length; l++)
            {
                OleDbCommand markAttedance = new OleDbCommand("UPDATE MarkAttendance SET '" + dtp.Text + "'= '" + p + "' WHERE RollNo=" + rno[l].ToString() + ";", markatt);
                markAttedance.ExecuteNonQuery();
                RefreshDBConnection();
            }
            
            MessageBox.Show(AttendBranch + " " + AttendDate + " " + AttendSem + " " + AttendSub + " " + AttendYear);
        }

        private void dtp_ValueChanged(object sender, EventArgs e)
        {

        }

        private void btnReport_Click(object sender, EventArgs e)
        {
            /* OleDbConnection connection1 = new OleDbConnection();
             connection1.ConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=Attendance\CMPN\sem 1\Attendance.mdb";
            
             OleDbCommand command = new OleDbCommand();
             command.CommandText = "select * from parts";
             using (OleDbDataAdapter dataAdapter = new OleDbDataAdapter(command.CommandText, connection1))
             {
                 DataTable ttttt = new DataTable();
                
                    
                     dataAdapter.Fill(ttttt);
                
             }*/
             Excel.Application xlApp;

             Excel.Workbook xlWorkBook;

             Excel.Worksheet xlWorkSheet;

             object misValue = System.Reflection.Missing.Value;



             xlApp = new Excel.Application();

             xlWorkBook = xlApp.Workbooks.Add(misValue);

             xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);
            
             OleDbConnection connection1 = new OleDbConnection();
             connection1.ConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=Attendance\" + cbBranch.Text + @"\" + cbSem.Text + @"\Attendance.mdb";


             DataTable tttab = new DataTable(); ;
             OleDbDataAdapter dataAdapter1 = new OleDbDataAdapter("select * from MarkAttendance", markatt);
            
             dataAdapter1.Fill(tttab);

             for (ia = 0; ia <= tttab.Rows.Count - 1; ia++)
             {

                 for (ja = 0; ja <= tttab.Columns.Count - 1; ja++)
                 {

                     data = tttab.Rows[ia].ItemArray[ja].ToString();

                     xlWorkSheet.Cells[ia + 1, ja + 1] = data;

                 }

             }

             xlWorkBook.SaveAs("csharp.net-informations.xls", Excel.XlFileFormat.xlWorkbookNormal, misValue, misValue, misValue, misValue, Excel.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);

             xlWorkBook.Close(true, misValue, misValue);

             xlApp.Quit();

             MessageBox.Show("Excel file created , you can find the file c:\\csharp.net-informations.xls");
             
        }



    }
}
