using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Threading;
using AXISMEDIACONTROLLib;
using System.Diagnostics;


namespace sw_mariscope
{
    public partial class Form1 : Form
    {
        #region declarations
        // NMEA interpreter
        NmeaInterpreter xGPS = new NmeaInterpreter();
        // OSGridConverter
        NMEA2OSG OSGconv = new NMEA2OSG();

        // The main control for communicating through the RS-232 port
        private SerialPort comport = new SerialPort("COM1", 4800, Parity.None, 8, StopBits.One);
        public string[] gpsString;
        public string instring;
        public string[] nrthest;
        public double ellipHeight;

        #endregion
        public Form1()
        {
            InitializeComponent();

            // Restore the users settings

            InitialiseControlValues();
            comport.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            xGPS.PositionReceived += new NmeaInterpreter.PositionReceivedEventHandler(GPS_PositionReceived);

        }

        #region serialport
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            this.Invoke(new EventHandler(HandleGPSstring));
        }
        private void HandleGPSstring(object s, EventArgs e)
        {
            string inbuff;
            inbuff = comport.ReadExisting();
            if (inbuff != null)
            {
                if (inbuff.StartsWith("$"))
                {
                    instring = inbuff;
                }
                else
                {
                    instring += inbuff;
                }
                gpsString = instring.Split();
                foreach (string item in gpsString) xGPS.Parse(item);
            }
        }

        #endregion
        //VARIABLES GLOBALES 
        string ip_server = "http://192.168.2.251";
        string user = "root";
        string password = "admin";

        //FIN VARIABLES GLOBALES

        private void button1_Click_1(object sender, EventArgs e)
        {
            AMC.Stop();
            string saveUrl = ip_server + "/axis-cgi/mjpg/video.cgi?resolution=" + cbxRes.SelectedValue.ToString() + "&compression=" + txtCompr.Text + "&camera=1";
            AMC.MediaURL = saveUrl;
            AMC.MediaType = "MJPEG";
            AMC.Play();
        }

        private void InitialiseControlValues()
        {
            cbxPortCom.Items.Clear();
            foreach (string s in SerialPort.GetPortNames())
                cbxPortCom.Items.Add(s);

            if (cbxPortCom.Items.Count > 0) cbxPortCom.SelectedIndex = 0;
            else
            {
                MessageBox.Show(this, "There are no COM Ports detected on this computer.\nPlease install a COM Port and restart this app.\n However, you can continue using the application. ", "No COM Ports Installed", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                //this.Close();
            }
        }

        private void btnOverlayBot_Click(object sender, EventArgs e)
        {
            AMC.Stop();
            AMC.EnableOverlays = false;
            //string urlOver = $"{ip_server}/axis-cgi/param.cgi?action=update&Image.I0.Text.TextEnabled=yes&Image.I0.Text.String={txtOverlay.Text}&Image.I0.Text.Position={cbxPlaceText.Text}&Image.I0.Text.Color={cbxColorText.Text}&Image.I0.Text.BGColor={cbxBKcolor.Text}&camera=1";
            string urlOver = $"{ip_server}/axis-cgi/param.cgi?action=update&Image.I0.Text.TextEnabled=yes&Image.I0.Text.String={txtOverlay.Text}&Image.I0.Text.Position={cbxPlaceText.Text}&Image.I0.Text.Color={cbxColorText.Text}&Image.I0.Text.BGColor={cbxBKcolor.Text}";
            NetworkCredential networkCredential = new NetworkCredential(user, password);
            WebRequest request = WebRequest.Create(urlOver);
            request.Credentials = networkCredential;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            response.Close();
            AMC.Play();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cbxBKcolor.SelectedIndex = 1;
            cbxColorText.SelectedIndex = 0;
            cbxPlaceText.SelectedIndex = 0;
            cbxBKcolor.SelectedIndex = 1;
            txtOverlay.Text = "Mariscope Ingeniería ";
            lblLatitude.Text = "-";
            lblLongitude.Text = "-";
            lblEstatePort.ForeColor = Color.IndianRed;
            lblEstatePort.Text = "¡Port Closed!";
            lblRecording.Text = "";

            if (cbxPortCom.Text == "")
            {
                btnOpenPort.Enabled = false;
                button2.Enabled = false;
            }
            else btnOpenPort.Enabled = true;
            //-------------------------------------------------
            //Inicio de camara al iniciar software   ||
            //-------------------------------------------------
            string url = "/axis-cgi/mjpg/video.cgi";
            string urlDef = ip_server + url;
            AMC.MediaURL = urlDef;
            AMC.MediaType = "MJPEG";
            AMC.Play();

            //-------------------------------------------------
            //Creacion de ruta guardado de imagenes y videos   ||
            //-------------------------------------------------

            string ruta = @"C:\swdemo\";

            if (!Directory.Exists(ruta))
            {
                DirectoryInfo di = Directory.CreateDirectory(ruta);
            }
            //-------------------------------------------------

            //-----Llenado combobox entorno de luz-------
            cbxWhitebalance.DisplayMember = "Text";
            cbxWhitebalance.ValueMember = "Value";
            cbxWhitebalance.Items.Add(new { Text = "Low", Value = "reportA" });
            cbxWhitebalance.Items.Add(new { Text = "Medium", Value = "reportB" });
            cbxWhitebalance.Items.Add(new { Text = "High", Value = "reportC" });
            cbxWhitebalance.Items.Add(new { Text = "Higher", Value = "reportD" });
            cbxWhitebalance.Items.Add(new { Text = "Extrem", Value = "reportE" });
            //-----------Fin llenado ---------------

            //------Llenado  combobox resolution ------------
            cbxRes.DisplayMember = "Text";
            cbxRes.ValueMember = "Value";

            var itemsRes = new[] {
             new { Text = "1400x1050 (16:9)" , Value = "1400x1050" },
             new { Text = "1280x720 (16:9)"  , Value = "1280x960"  },
             new { Text = "1024x768 (4:3)"   , Value = "1024x768"  },
             new { Text = "800x600 (4:3)"    , Value = "800x600"   },
             new { Text = "640x480 (4:3)"    , Value = "640x480"   },
             new { Text = "480x360 (4:3)"    , Value = "480x360"   },
             new { Text = "320x240 (4:3)"    , Value = "320x240"   },
             new { Text = "240x180(4:3)"     , Value = "240x180"   },
             new { Text = "160x120 (4:3)"    , Value = "160x120"   },
             new { Text = "1920x1080 (16:9)" , Value = "1920x1080" },
             new { Text = "1280x720 (16:9)"  , Value = "1280x720"  },
             new { Text = "854x480 (16:9)"   , Value = "854x480"   },
             new { Text = "800x450 (16:9)"   , Value = "800x450"   },
             new { Text = "640x360 (16:9)"   , Value = "640x360"   },
             new { Text = "320x180 (16:9)"   , Value = "320x180"   },
             new { Text = "160x90 (16:9)"    , Value = "160x90"    },
            };

            cbxRes.DataSource = itemsRes;
            //------Llenado  combobox resolution ------------

            //------Llenado White Balance ------------
            cbxWhitebalance.DisplayMember = "Text";
            cbxWhitebalance.ValueMember = "Value";

            var itemsWB = new[] {
             new { Text = "Automatic" , Value = "auto" },
             new { Text = "Automatic Outdoor"  , Value = "auto_outdoor"  },
             new { Text = "Hold Current"  , Value = "hold"  },
             new { Text = "Fixed Outdoor 1"   , Value = "fixed_outdoor1"   },
             new { Text = "Fixed Outdoor 2"   , Value = "fixed_outdoor2"   },
             new { Text = "Fixed Indoor"   , Value = "fixed_indoor"   },
             new { Text = "Fixed Fluorescent 1"   , Value = "fixed_fluor1"   },
             new { Text = "Fixed Fluorescent 2"   , Value = "fixed_fluor2"   },
             new { Text = "Manual"   , Value = "manual"   },

            };
            cbxWhitebalance.DataSource = itemsWB;
            //------Llenado  Llenado White Balance ------------

            //------Llenado Zone Exposure ------------
            cbxExposureZones.DisplayMember = "Text";
            cbxExposureZones.ValueMember = "Value";
            var itemsEZ = new[] {
             new { Text = "Auto"  , Value = "auto"},
             new { Text = "Right" , Value = "right"},
             new { Text = "Left"  , Value = "left"},
             new { Text = "Upper" , Value = "upper"},
             new { Text = "Lower" , Value = "lower"},
             new { Text = "Spot"  , Value = "spot"}
            };
            cbxExposureZones.DataSource = itemsEZ;
            //------Llenado Zone Exposure ------------

            //------Llenado Exposure Control ------------
            cbxExposureControl.DisplayMember = "Text";
            cbxExposureControl.ValueMember = "Value";
            var itemsEC = new[] {
             new { Text = "Automatic"  , Value = "auto"},
             new { Text = "Flicker-reduced 50 Hz" , Value = "flickerreduced50"},
             new { Text = "Flicker-reduced 60 Hz"  , Value = "flickerreduced60"},
             new { Text = "Hold current" , Value = "hold"}
            };
            cbxExposureControl.DataSource = itemsEC;
            //------Llenado Exposure Control ------------

            //------Llenado GAIN MAX ------------
            cbxGainMAX.DisplayMember = "Text";
            cbxGainMAX.ValueMember = "Value";
            var itemShutterMax = new[] {
             new { Text = "0"   , Value = "0"},
             new { Text = "3"   , Value = "8"},
             new { Text = "6"   , Value = "17"},
             new { Text = "9"   , Value = "25"},
             new { Text = "12"  , Value = "33"},
             new { Text = "15"  , Value = "42"},
             new { Text = "18"  , Value = "50"},
             new { Text = "21"  , Value = "58"},
             new { Text = "24"  , Value = "67"},
             new { Text = "27"  , Value = "75"},
             new { Text = "30"  , Value = "83"},
             new { Text = "33"  , Value = "92"},
             new { Text = "36"  , Value = "100"},

            };
            cbxGainMAX.DataSource = itemShutterMax;
            //------Llenado GAIN MAX ------------

        }

        private void button5_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblHora.Text = DateTime.Now.ToLongTimeString();
            lblFecha.Text = DateTime.Now.ToShortDateString();
        }

        private void trackBar3_Scroll_1(object sender, EventArgs e)
        {
            txtContrast.Text = tckContrast.Value.ToString();
        }

        private void trackBar2_Scroll_1(object sender, EventArgs e)
        {
            txtBright.Text = tckBright.Value.ToString();
        }

        private void btnImage_Click_1(object sender, EventArgs e)
        {
            AMC.Stop();

            //BRIGHTNESS 
            string urlBright = ip_server + "/axis-cgi/param.cgi?action=update&ImageSource.I0.Sensor.brightness=" + txtBright.Text;
            NetworkCredential networkCredential = new NetworkCredential(user, password);
            WebRequest request = WebRequest.Create(urlBright);
            request.Credentials = networkCredential;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            response.Close();
            //FIN BRIGHTNESS 

            //CONTRAST
            string urlContrast = ip_server + "/axis-cgi/param.cgi?action=update&ImageSource.I0.Sensor.Contrast=" + txtContrast.Text;
            NetworkCredential networkCredential2 = new NetworkCredential(user, password);
            WebRequest request2 = WebRequest.Create(urlContrast);
            request2.Credentials = networkCredential2;
            HttpWebResponse response2 = (HttpWebResponse)request2.GetResponse();
            response2.Close();
            //FIN CONTRAST

            //SATURATION
            string urlSaturation = ip_server + "/axis-cgi/param.cgi?action=update&ImageSource.I0.Sensor.Colordesaturation=" + txtSatura.Text;
            NetworkCredential networkCredential3 = new NetworkCredential(user, password);
            WebRequest request3 = WebRequest.Create(urlSaturation);
            request3.Credentials = networkCredential3;
            HttpWebResponse response3 = (HttpWebResponse)request3.GetResponse();
            response3.Close();
            //FIN SATURATION

            //SHARPNESS
            string urlSharpness = ip_server + "/axis-cgi/param.cgi?action=update&ImageSource.I0.Sensor.Sharpness=" + txtSharp.Text;
            NetworkCredential networkCredential4 = new NetworkCredential(user, password);
            WebRequest request4 = WebRequest.Create(urlSharpness);
            request4.Credentials = networkCredential4;
            HttpWebResponse response4 = (HttpWebResponse)request4.GetResponse();
            response4.Close();
            //FIN SHARPNESS

            //WHITE BALANCE
            string urlWB = ip_server + "/axis-cgi/param.cgi?action=update&ImageSource.I0.Sensor.WhiteBalance=" + cbxWhitebalance.SelectedValue.ToString();
            NetworkCredential networkCredential5 = new NetworkCredential(user, password);
            WebRequest request5 = WebRequest.Create(urlWB);
            request5.Credentials = networkCredential5;
            HttpWebResponse response5 = (HttpWebResponse)request5.GetResponse();
            response5.Close();
            //FIN WHITE BALANCE

            //EXPOSURE VALUE
            string urlEV = ip_server + "/axis-cgi/param.cgi?action=update&ImageSource.I0.Sensor.ExposureValue=" + txtExposureValue.Text;
            NetworkCredential networkCredential6 = new NetworkCredential(user, password);
            WebRequest request6 = WebRequest.Create(urlEV);
            request6.Credentials = networkCredential6;
            HttpWebResponse response6 = (HttpWebResponse)request6.GetResponse();
            response6.Close();
            //FIN EXPOSURE VALUE

            //Exposure zones
            string urlEZ = ip_server + "/axis-cgi/param.cgi?action=update&ImageSource.I0.Sensor.ExposureWindow=" + cbxExposureZones.SelectedValue.ToString();
            NetworkCredential networkCredential7 = new NetworkCredential(user, password);
            WebRequest request7 = WebRequest.Create(urlEZ);
            request7.Credentials = networkCredential7;
            HttpWebResponse response7 = (HttpWebResponse)request7.GetResponse();
            response7.Close();
            //FIN Exposure zones

            //Exposure Control
            string urlEC = ip_server + "/axis-cgi/param.cgi?action=update&ImageSource.I0.Sensor.Exposure=" + cbxExposureZones.SelectedValue.ToString();
            NetworkCredential networkCredential8 = new NetworkCredential(user, password);
            WebRequest request8 = WebRequest.Create(urlEC);
            request8.Credentials = networkCredential8;
            HttpWebResponse response8 = (HttpWebResponse)request8.GetResponse();
            response8.Close();
            //FIN Exposure Control

            //gain max
            string urlShutterMAX = ip_server + "/axis-cgi/param.cgi?action=update&ImageSource.I0.Sensor.ManGainVal=" + cbxGainMAX.SelectedValue.ToString();
            NetworkCredential networkCredential9 = new NetworkCredential(user, password);
            WebRequest request9 = WebRequest.Create(urlShutterMAX);
            request9.Credentials = networkCredential9;
            HttpWebResponse response9 = (HttpWebResponse)request9.GetResponse();
            response9.Close();
            //FIN gain max

            AMC.Play();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            AMC.FullScreen = true;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            string ruta = @"C:\swdemo\image";
            string rutaSnap = @"C:\swdemo\image\";
            string formato = ".jpg";
            string DateAndTime = DateTime.Now.ToString("MM-dd-yyyy H.mm.ss");


            string rutaDef = rutaSnap + DateAndTime.ToString() + formato;

            if (!Directory.Exists(ruta))
            {
                DirectoryInfo di = Directory.CreateDirectory(ruta);
            }
            else
            {
                AMC.SaveCurrentImage(0, rutaDef);

                if (File.Exists(rutaDef))
                {
                    AMC.SaveCurrentImage(0, rutaDef);
                }
            }
        }

            
        private void button9_Click(object sender, EventArgs e)
        {
            string DateAndTime = DateTime.Now.ToString("MM-dd-yyyy H.mm.ss");

            string folderVideo = @"C:\swdemo\video";

                if (!Directory.Exists(folderVideo))
                {
                    DirectoryInfo di = Directory.CreateDirectory(folderVideo);
                }

                string rutaGuardado = @"C:\swdemo\video\";
                string formato = ".asf";

                string dir = rutaGuardado + DateAndTime + formato;

                AMC.StartRecordMedia(dir, 8, "0");
                
                btnRecord.ForeColor = Color.Green;
                btnRecord.Enabled = false;
                btnStopRecord.Enabled = true;
                lblRecording.Text = "Recording ...";

            
        }

        private void button10_Click(object sender, EventArgs e)
        {
            AMC.StopRecordMedia();
            btnRecord.Enabled = true;
            btnStopRecord.Enabled = false;
            lblRecording.Text = "";
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            txtCompr.Text = trackBar1.Value.ToString();
        }

        private void txtCompr_TextChanged(object sender, EventArgs e)
        {
            txtCompr.Text = trackBar1.Value.ToString();
        }

        private void trackBar7_Scroll(object sender, EventArgs e)
        {
            txtSatura.Text = tckSatur.Value.ToString();
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            txtSharp.Text = tckSharp.Value.ToString();
        }

        private void chkDate_CheckedChanged(object sender, EventArgs e)
        {
            txtOverlay.Text += " %D";
        }

        private void chkTime_CheckedChanged(object sender, EventArgs e)
        {
            txtOverlay.Text += " %T";
        }

        private void trkbExposureValue_Scroll(object sender, EventArgs e)
        {
            txtExposureValue.Text = trkbExposureValue.Value.ToString();
        }


        private void button3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            else {
                this.WindowState = FormWindowState.Maximized;
            }
        }
        public int xClick = 0, yClick = 0;

        private void txtSharp_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSharp.Text) || Convert.ToInt32(txtSharp.Text) >= 100) {
                txtSharp.Text = "";
            }
            else {
                tckSharp.Value = Convert.ToInt32(txtSharp.Text);
                tckSharp.Value += 1;
                if (tckSharp.Value == tckSharp.Maximum) {

                    tckSharp.Value = tckSharp.Minimum;
                }
            }

        }

        private void txtSharp_KeyPress(object sender, KeyPressEventArgs e)
        {
            // SOLO NUMEROS 
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
                lblSharpError.Visible = true;
                lblSharpError.Text = "Just Numbers!";
            }
            else {
                lblSharpError.Visible = false;
            }
        }

        private void txtSatura_KeyPress(object sender, KeyPressEventArgs e)
        {
            // SOLO NUMEROS 
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
                lblSaturError.Visible = true;
                lblSaturError.Text = "Just Numbers!";
            }
            else
            {
                lblSaturError.Visible = false;
            }
        }

        private void txtSatura_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSatura.Text) || Convert.ToInt32(txtSatura.Text) >= 100)
            {
                txtSatura.Text = "";
            }
            else
            {
                tckSatur.Value = Convert.ToInt32(txtSatura.Text);
                tckSatur.Value += 1;
                if (tckSatur.Value == tckSatur.Maximum)
                {
                    tckSatur.Value = tckSatur.Minimum;
                }
            }
        }

        private void txtContrast_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtContrast.Text) || Convert.ToInt32(txtContrast.Text) >= 100)
            {
                txtContrast.Text = "";
            }
            else
            {
                tckContrast.Value = Convert.ToInt32(txtContrast.Text);
                tckContrast.Value += 1;
                if (tckContrast.Value == tckContrast.Maximum)
                {
                    tckContrast.Value = tckContrast.Minimum;
                }
            }
        }

        private void txtContrast_KeyPress(object sender, KeyPressEventArgs e)
        {
            // SOLO NUMEROS 
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
                lblCntrError.Visible = true;
                lblCntrError.Text = "Just Numbers!";
            }
            else
            {
                lblCntrError.Visible = false;
            }
        }

        private void txtBright_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtBright.Text) || Convert.ToInt32(txtBright.Text) >= 100)
            {
                txtBright.Text = "";
            }
            else
            {
                tckBright.Value = Convert.ToInt32(txtBright.Text);
                tckBright.Value += 1;
                if (tckBright.Value == tckBright.Maximum)
                {
                    tckBright.Value = tckBright.Minimum;
                }
            }
        }


        private void txtBright_KeyPress(object sender, KeyPressEventArgs e)
        {
            // SOLO NUMEROS 
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
                lbltBrightError.Visible = true;
                lbltBrightError.Text = "Just Numbers!";
            }
            else
            {
                lbltBrightError.Visible = false;
            }
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", @"C:\swdemo\");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.mariscope.cl/");
        }


        private void button2_Click_1(object sender, EventArgs e)
        {
            string Date = DateTime.Now.ToString("MM/dd/yyyy");
            string time = DateTime.Now.ToString("HH/mm/ss");
            string ruta = @"C:\swdemo\image";
            string rutaSnap = @"C:\swdemo\image\";
            string formato = ".jpg";

            string rutaDef = rutaSnap + Date + "-" + time + formato;

            if (!Directory.Exists(ruta))
            {
                DirectoryInfo di = Directory.CreateDirectory(ruta);
            }
            else
            {
                AMC.SaveCurrentImage(0, rutaDef);

                if (File.Exists(rutaDef))
                {
                    AMC.SaveCurrentImage(0, rutaDef);
                }
            }

        }

        private void chkIncludeTime_CheckedChanged(object sender, EventArgs e)
        {
            if (chkIncludeTime.Checked == true)
            {
                txtOverlay.Text += " %T";
            }

            else {
                txtOverlay.Text = txtOverlay.Text.Replace(" %T", string.Empty);
            }
        }

        private void chkIncludeDate_CheckedChanged(object sender, EventArgs e)
        {
            if (chkIncludeDate.Checked == true)
            {
                txtOverlay.Text += " %D";
            }

            else
            {
                txtOverlay.Text = txtOverlay.Text.Replace(" %D", string.Empty);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            AMC.Stop();
            string urlOverlay = ip_server + "/axis-cgi/param.cgi?action=update&Image.I0.Text.Textenabled=no";
            NetworkCredential networkCredential2 = new NetworkCredential(user, password);
            WebRequest request2 = WebRequest.Create(urlOverlay);
            request2.Credentials = networkCredential2;
            HttpWebResponse response2 = (HttpWebResponse)request2.GetResponse();
            response2.Close();
            txtOverlay.Clear();
            cbxBKcolor.SelectedIndex = 1;
            cbxColorText.SelectedIndex = 0;
            cbxPlaceText.SelectedIndex = 0;
            cbxBKcolor.SelectedIndex = 1;

            chkIncludeTime.Checked = false;
            chkIncludeDate.Checked = false;
            AMC.Play();
        }

        private void btnOpenPort_Click(object sender, EventArgs e)
        {
            if (comport.IsOpen)
            {
                comport.Close();
                btnOpenPort.Text = "Open Port ";
                lblLongitude.Text = "-";
                lblLatitude.Text = "-";
                lblEstatePort.Text = "¡Port Closed!";
                lblEstatePort.ForeColor = Color.IndianRed;
            }
            else
            {
                // Set the port's settings
                
                comport.PortName = cbxPortCom.Text;
                // Open the port
                comport.Open();
                btnOpenPort.Text = "Close Port";
                lblEstatePort.Text = "¡Port Open!";
                lblLatitude.ForeColor = Color.GreenYellow;
                lblLongitude.ForeColor = Color.GreenYellow;
                lblEstatePort.ForeColor = Color.DarkSeaGreen;
               
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            { xClick = e.X; yClick = e.Y; }
            else
            { this.Left = this.Left + (e.X - xClick); this.Top = this.Top + (e.Y - yClick); }
        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            string coordinates = $"Lat: {lblLatitude.Text} Lon: {lblLongitude.Text}";

            if (btnOpenPort.Text == "") {
                button2.Enabled = false;
            }
            else { 
                txtOverlay.Text += coordinates;
            }
        }


        #region GPS data
        private void GPS_PositionReceived(string Lat, string Lon)
        {
            lblLatitude.Text = Lat;
            lblLongitude.Text = Lon;
        }
        #endregion

      
    }
}
