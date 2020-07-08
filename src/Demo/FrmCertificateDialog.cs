using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;

namespace Demo
{
    public partial class FrmCertificateDialog : Form
    {
        private X509Certificate2 selCert = null;
        /// <summary>
        /// 证书密码
        /// </summary>
        private string certPassword = null;
        /// <summary>
        /// 选中的证书
        /// </summary>
        public X509Certificate2 SelectedCert
        {
            get
            {
                return selCert;
            }
        }
        ///// <summary>
        ///// 证书密码
        ///// </summary>
        //public string Password
        //{
        //    get
        //    {
        //        return certPassword;
        //    }
        //}

        public FrmCertificateDialog()
        {
            InitializeComponent();
            Init();
        }
        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            X509Store store = new X509Store(StoreName.My);
            store.Open(OpenFlags.ReadOnly);
            foreach (X509Certificate2 cert in store.Certificates)
            {
                certsListBox.Items.Add(cert);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            //certPassword = passwordBox.Text.Trim();
        }

        private void certsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            selCert = certsListBox.SelectedItem as X509Certificate2;
            if (selCert != null)
            {
                try
                {
                    string subject = selCert.Subject;
                    lbUser.Text = subject;
                }
                catch (Exception ex)
                {

                }
            }
        }
    }
}
