using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Core;
using log4net.Appender;
using log4net.Layout;
using log4net.Config;
using log4net.Repository.Hierarchy;

namespace Utill
{
    public static class LogManager
    {   
        public static void Write(string str)
        {
            try
            {
                string logFolderPath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\System Log\\";

                if (!Directory.Exists(logFolderPath))
                {
                    Directory.CreateDirectory(logFolderPath);
                }
                
                DateTime dateTime = DateTime.Now;

                if (dateTime.CompareTo(new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 15, 0, 0)) > 0)
                {
                    
                }
                else
                {

                }

                string filePath = logFolderPath + dateTime.ToString("yyyyMMdd") + ".log";

                StreamWriter writer = File.AppendText(filePath);
                writer.WriteLine(dateTime.ToString("HHmmss fff") + "\t" + str);

                Console.WriteLine(dateTime.ToString("yyyy-MM-dd HH:mm:ss fff") + "\t" + str);
                writer.Close();
            }
            catch
            {

            }
        }

        private static ILog log = log4net.LogManager.GetLogger("Program");

        //Custom Log Level
        private static readonly Level traceLevel = new Level(10, "Trace");
        private static readonly Level actionLevel = new Level(11, "Action");

        static void Main(string[] args)
        {
            Console.ReadLine();
        }

        /// <summary>
        /// 로그를 기입하기 위한 매니저
        /// </summary>
        /// <param name="isShowClassName">로그가 발생한 Class명 표시 여부</param>
        /// <param name="isShowMethod">로그가 발생한 Method명 표시 여부</param>
        static LogManager()
        {
            bool isShowClassName = true;
            bool isShowMethod = true;

            var repository = log4net.LogManager.GetRepository();
            if (repository.Configured)
            {
                return;
            }
            repository.Configured = true;

            repository.LevelMap.Add(traceLevel);
            repository.LevelMap.Add(actionLevel);

            string consolePattern = "%d{yyyy-MM-dd HH:mm:ss} [%thread]";

            if (isShowClassName)
            {
                consolePattern += " [%C]";
            }
            if (isShowMethod)
            {
                consolePattern += " [%M]";
            }

            consolePattern += " %level - %message%newline";

            // 컬러 콘솔 로그 패턴 설정
            var appender = new ColoredConsoleAppender
            {
                Threshold = Level.All,
                Layout = new PatternLayout(consolePattern),
            };
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Debug,
                ForeColor = ColoredConsoleAppender.Colors.White
                    | ColoredConsoleAppender.Colors.HighIntensity
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Info,
                ForeColor = ColoredConsoleAppender.Colors.Green
                    | ColoredConsoleAppender.Colors.HighIntensity
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Warn,
                ForeColor = ColoredConsoleAppender.Colors.Purple
                    | ColoredConsoleAppender.Colors.HighIntensity
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Error,
                ForeColor = ColoredConsoleAppender.Colors.Red
                    | ColoredConsoleAppender.Colors.HighIntensity
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Fatal,
                ForeColor = ColoredConsoleAppender.Colors.White
                    | ColoredConsoleAppender.Colors.HighIntensity,
                BackColor = ColoredConsoleAppender.Colors.Red
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = actionLevel,
                ForeColor = ColoredConsoleAppender.Colors.Yellow
                    | ColoredConsoleAppender.Colors.HighIntensity
            });
            appender.ActivateOptions();
            BasicConfigurator.Configure(appender);

            // 파일 로그 패턴 설정
            var rollingAppender = new RollingFileAppender();
            rollingAppender.Name = "RollingFile";

            // 시스템이 기동되면 파일을 추가해서 할 것인가? 새로 작성할 것인가?
            rollingAppender.AppendToFile = true;

            rollingAppender.DatePattern = "yyyy년MM월dd일.lo\\g";
            // 로그 파일 설정
            rollingAppender.File = @"Log\";
            rollingAppender.StaticLogFileName = false;

            // 파일 단위 날짜 또는 파일 사이즈로 설정.
            rollingAppender.RollingStyle = RollingFileAppender.RollingMode.Date;

            string rollingPattern = "%d [%t] ";
            if (isShowClassName)
            {
                rollingPattern += "[%C]";
            }
            if (isShowMethod)
            {
                rollingPattern += "[%M]";
            }
            rollingPattern += "%-5p %c - %m%n";

            rollingAppender.Layout = new PatternLayout(rollingPattern);
            var hierarchy = (Hierarchy)repository;
            hierarchy.Root.AddAppender(rollingAppender);
            rollingAppender.ActivateOptions();

            hierarchy.Root.Level = log4net.Core.Level.All;

        }

        /// <summary>
        /// 시스템의 동작을 추적하기 위한 Log를 남길 시 사용.
        /// </summary>
        /// <param name="msg">표시할 메시지</param>
        public static void Trace(string msg)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, traceLevel, msg, null);
        }

        /// <summary>
        /// 사용자 또는 외부의 제어 및 동작에 대한 Log를 남길 시 사용.
        /// </summary>
        /// <param name="msg">표시할 메시지</param>
        public static void Action(string msg)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, actionLevel, msg, null);
        }

        /// <summary>
        /// 정보를 Log에 남길 시 사용.
        /// </summary>
        /// <param name="msg">표시할 메시지</param>
        public static void Info(string msg)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, Level.Info, msg, null);
        }

        /// <summary>
        /// 치명적인 에러 발생시 남길 Log에 사용.
        /// </summary>
        /// <param name="msg">표시할 메시지</param>
        public static void Fatal(string msg)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, Level.Fatal, msg, null);
        }

        /// <summary>
        /// 디버깅시 Log 출력시 사용.
        /// </summary>
        /// <param name="msg">표시할 메시지</param>
        public static void Debug(string msg)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, Level.Debug, msg, null);
        }

        /// <summary>
        /// 경고 Log 출력시 사용.
        /// </summary>
        /// <param name="msg">표시할 메시지</param>
        public static void Warn(string msg)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, Level.Warn, msg, null);
        }

        /// <summary>
        /// 에러 메시지 출력시 사용.
        /// </summary>
        /// <param name="msg">표시할 메시지</param>
        public static void Error(string msg)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, Level.Error, msg, null);
        }
    }
}
