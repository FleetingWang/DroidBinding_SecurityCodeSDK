using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using static Android.Views.View;
using CN.Smssdk;
using Android;
using Android.Content.PM;
using System.Collections.Generic;
using Android.Preferences;
using CN.Smssdk.Gui;
using static Android.OS.Handler;

namespace SMSSDKSample
{
    //请注意：测试短信条数限制发送数量：20条/天，APP开发完成后请到mob.com后台提交审核，获得不限制条数的免费短信权限。
    [Activity(Label = "SMSSDKSample", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, IOnClickListener, ICallback
    {
        // 填写从短信SDK应用后台注册得到的APPKEY
        //此APPKEY仅供测试使用，且不定期失效，请到mob.com后台申请正式APPKEY
        private static string APPKEY = "f3fc6baa9ac4";

        // 填写从短信SDK应用后台注册得到的APPSECRET
        private static string APPSECRET = "7f3dedcb36d92deebcb373af921d635a";

        // 短信注册，随机产生头像
        private static string[] AVATARS = {
		    "http://tupian.qqjay.com/u/2011/0729/e755c434c91fed9f6f73152731788cb3.jpg",
            //该图片已失效。"http://99touxiang.com/public/upload/nvsheng/125/27-011820_433.jpg",
		    "http://img1.touxiang.cn/uploads/allimg/111029/2330264224-36.png",
		    "http://img1.2345.com/duoteimg/qqTxImg/2012/04/09/13339485237265.jpg",
		    "http://diy.qqjay.com/u/files/2012/0523/f466c38e1c6c99ee2d6cd7746207a97a.jpg",
		    "http://img1.touxiang.cn/uploads/20121224/24-054837_708.jpg",
		    "http://img1.touxiang.cn/uploads/20121212/12-060125_658.jpg",
		    "http://img1.touxiang.cn/uploads/20130608/08-054059_703.jpg",
		    "http://diy.qqjay.com/u2/2013/0422/fadc08459b1ef5fc1ea6b5b8d22e44b4.jpg",
		    "http://img1.2345.com/duoteimg/qqTxImg/2012/04/09/13339510584349.jpg",
		    "http://img1.touxiang.cn/uploads/20130515/15-080722_514.jpg",
		    "http://diy.qqjay.com/u2/2013/0401/4355c29b30d295b26da6f242a65bcaad.jpg"
	    };

        private bool ready;
        private bool gettingFriends;
        private Dialog pd;
        private TextView tvNum;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.main_activity);

            Button btnRegist = FindViewById<Button>(Resource.Id.btn_bind_phone);
            View btnContact = FindViewById<View>(Resource.Id.rl_contact);
            tvNum = FindViewById<TextView>(Resource.Id.tv_num);
            tvNum.Visibility = ViewStates.Gone;
            btnRegist.SetOnClickListener(this);
            btnContact.SetOnClickListener(this);
            gettingFriends = false;

            loadSharePrefrence();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                Permission readPhone = CheckSelfPermission(Manifest.Permission.ReadPhoneState);
                Permission receiveSms = CheckSelfPermission(Manifest.Permission.ReceiveSms);
                Permission readSms = CheckSelfPermission(Manifest.Permission.ReadSms);
                Permission readContacts = CheckSelfPermission(Manifest.Permission.ReadContacts);
                Permission readSdcard = CheckSelfPermission(Manifest.Permission.ReadExternalStorage);

                int requestCode = 0;
                List<String> permissions = new List<String>();
                if (readPhone != Permission.Granted)
                {
                    requestCode |= 1 << 0;
                    permissions.Add(Manifest.Permission.ReadPhoneState);
                }
                if (receiveSms != Permission.Granted)
                {
                    requestCode |= 1 << 1;
                    permissions.Add(Manifest.Permission.ReceiveSms);
                }
                if (readSms != Permission.Granted)
                {
                    requestCode |= 1 << 2;
                    permissions.Add(Manifest.Permission.ReadSms);
                }
                if (readContacts != Permission.Granted)
                {
                    requestCode |= 1 << 3;
                    permissions.Add(Manifest.Permission.ReadContacts);
                }
                if (readSdcard != Permission.Granted)
                {
                    requestCode |= 1 << 4;
                    permissions.Add(Manifest.Permission.ReadExternalStorage);
                }
                if (requestCode > 0)
                {
                    string[] permission = new string[permissions.Count];
                    this.RequestPermissions(permissions.ToArray(), requestCode);
                    return;
                }
            }

            showAppkeyDialog();

        }

        private void showAppkeyDialog()
        {
            Dialog dialog = new Dialog(this, Resource.Style.CommonDialog);
            dialog.SetContentView(Resource.Layout.smssdk_set_appkey_dialog);
            EditText etAppKey = dialog.FindViewById<EditText>(Resource.Id.et_appkey);
            etAppKey.Text = APPKEY;
            EditText etAppSecret = dialog.FindViewById<EditText>(Resource.Id.et_appsecret);
            etAppSecret.Text = APPSECRET;

            dialog.FindViewById(Resource.Id.btn_dialog_ok).Click += (sender, e) => {
                APPKEY = etAppKey.Text.Trim();
                APPSECRET = etAppSecret.Text.Trim();
                if (string.IsNullOrEmpty(APPKEY) || string.IsNullOrEmpty(APPSECRET))
                {
                    Toast.MakeText(this, Resource.String.smssdk_appkey_dialog_title, ToastLength.Short).Show();
                }
                else
                {
                    dialog.Dismiss();
                    setSharePrefrence();
                    initSDK();
                }
            };
            dialog.FindViewById(Resource.Id.btn_dialog_cancel).Click += (sender, e) => {
                dialog.Dismiss();
                Finish();
            };
            dialog.SetCancelable(false);
            dialog.Show();
        }

        private void initSDK()
        {
            // 初始化短信SDK
            SMSSDK.InitSDK(this, APPKEY, APPSECRET, true);
            if (APPKEY.Equals("f3fc6baa9ac4", StringComparison.InvariantCultureIgnoreCase))
            {
                Toast.MakeText(this, "此APPKEY仅供测试使用，且不定期失效，请到mob.com后台申请正式APPKEY", ToastLength.Short).Show();
            }

            var eventHandler = new CustomEventHandler(new Handler(this));
            // 注册回调监听接口
            SMSSDK.RegisterEventHandler(eventHandler);
            ready = true;

            // 获取新好友个数
            showDialog();
            SMSSDK.GetNewFriendsCount();
            gettingFriends = true;
        }

        private void loadSharePrefrence()
        {
            ISharedPreferences p = GetSharedPreferences("SMSSDK_SAMPLE", FileCreationMode.Private);
            APPKEY = p.GetString("APPKEY", APPKEY);
            APPSECRET = p.GetString("APPSECRET", APPSECRET);
        }

        private void setSharePrefrence()
        {
            ISharedPreferences p = GetSharedPreferences("SMSSDK_SAMPLE", FileCreationMode.Private);
            ISharedPreferencesEditor edit = p.Edit();
            edit.PutString("APPKEY", APPKEY);
            edit.PutString("APPSECRET", APPSECRET);
            edit.Commit();
        }

        protected override void OnDestroy()
        {
            if (ready)
            {
                // 销毁回调监听接口
                SMSSDK.UnregisterAllEventHandler();
            }
            base.OnDestroy();
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (ready && !gettingFriends)
            {
                // 获取新好友个数
                showDialog();
                SMSSDK.GetNewFriendsCount();
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.btn_bind_phone:
                    // 打开注册页面
                    RegisterPage registerPage = new RegisterPage();
                    registerPage.SetRegisterCallback(new RegisterPageEventHander());
                    registerPage.Show(this);
                    break;
                case Resource.Id.rl_contact:
                    tvNum.Visibility = ViewStates.Gone;
                    // 打开通信录好友列表页面
                    ContactsPage contactsPage = new ContactsPage();
                    contactsPage.Show(this);
                    break;
            }
        }

        public bool HandleMessage(Message msg)
        {
            if (pd != null && pd.IsShowing)
            {
                pd.Dismiss();
            }

            int e = msg.Arg1;
            int result = msg.Arg2;
            Java.Lang.Object data = msg.Obj;
            if (e == SMSSDK.EventSubmitUserInfo)
            {
                // 短信注册成功后，返回MainActivity,然后提示新好友
                if (result == SMSSDK.ResultComplete)
                {
                    Toast.MakeText(this, Resource.String.smssdk_user_info_submited, ToastLength.Short).Show();
                }
                else
                {
                    //((Java.Lang.Throwable)data).PrintStackTrace();
                }
            }
            else if (e == SMSSDK.EventGetNewFriendsCount)
            {
                if (result == SMSSDK.ResultComplete)
                {
                    refreshViewCount(data);
                    gettingFriends = false;
                }
                else
                {
                    //((Java.Lang.Throwable)data).PrintStackTrace();
                }
            }
            return false;
        }

        // 更新，新好友个数
        private void refreshViewCount(Java.Lang.Object data)
        {
            int newFriendsCount = 0;
            try
            {
                newFriendsCount = Java.Lang.Integer.ParseInt(Java.Lang.String.ValueOf(data));
            }
            catch (Java.Lang.Throwable t)
            {
                newFriendsCount = 0;
            }
            if (newFriendsCount > 0)
            {
                tvNum.Visibility = ViewStates.Visible;
                tvNum.Text = Java.Lang.String.ValueOf(newFriendsCount);
            }
            else
            {
                tvNum.Visibility = ViewStates.Gone;
            }
            if (pd != null && pd.IsShowing)
            {
                pd.Dismiss();
            }
        }
        // 弹出加载框
        private void showDialog()
        {
            if (pd != null && pd.IsShowing)
            {
                pd.Dismiss();
            }
            pd = CommonDialog.ProgressDialog(this);
            pd.Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            showAppkeyDialog();
        }

        private class CustomEventHandler: CN.Smssdk.EventHandler
        {
            private Handler handler;
            public CustomEventHandler(Handler handler)
            {
                this.handler = handler;
            }

            public override void AfterEvent(int p0, int p1, Java.Lang.Object p2)
            {
                Message msg = new Message();
                msg.Arg1 = p0;
                msg.Arg2 = p1;
                msg.Obj = p2;
                handler.SendMessage(msg);
            }
        }

        private class RegisterPageEventHander: CN.Smssdk.EventHandler
        {
            public override void AfterEvent(int p0, int result, Java.Lang.Object data)
            {
                // 解析注册结果
                if (result == SMSSDK.ResultComplete)
                {
                    JavaDictionary<string, object> phoneMap =(JavaDictionary<string, object>)data;
                    string country = (string)phoneMap["country"];
                    string phone = (string)phoneMap["phone"];
                    // 提交用户信息
                    Random rnd = new Random();
                    int id = rnd.Next();
                    string uid = id.ToString();
                    string nickName = "SmsSDK_User_" + uid;
                    string avatar = AVATARS[id % AVATARS.Length];
                    SMSSDK.SubmitUserInfo(uid, nickName, avatar, country, phone);
                }
            }
        }
    }
}

