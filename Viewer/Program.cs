using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TextComposing.Printing
{

    class JapaneseFontMetric
    {
        public float VerticalWidth { get; set; }
        public float VerticalHeight { get; set; }
    }

    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string input;
            if (args.Length < 1)
            {
                Console.WriteLine("使用法：引数1 入力テキストファイルパス");
                return;
            }
            input = args[0];
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(input));
        }
    }
}
