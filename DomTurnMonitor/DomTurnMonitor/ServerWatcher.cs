using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace DomTurnMonitor
{
  public class IntEventArgs : EventArgs
  {
    public int Value { get; private set; }
    public IntEventArgs(int val) { Value = val; }
  }
  public class DateTimeEventArgs : EventArgs
  {
    public DateTime Value { get; private set; }
    public DateTimeEventArgs(DateTime val) { Value = val; }
  }

  class ServerWatcher
  {
    private string gameName;
    private System.Timers.Timer updateTimer;

    internal ServerWatcher(string gameName)
    {
      this.gameName = gameName;

      // perform an initial update
      Update();

      // TODO Control the interval of these timers for when the form is hidden. Wrap interface into Game, as EmailWatcher will need the same.
      updateTimer = new Timer(60 * 1000);
      updateTimer.Elapsed += updateTimer_Elapsed;
      updateTimer.Start();
    }

    public double UpdateInterval
    {
      get { return updateTimer.Interval; }
      set { updateTimer.Stop(); updateTimer.Interval = value; updateTimer.Start(); }
    }

    private void updateTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      updateTimer.Stop();
      Update();
      // restart the timer
      updateTimer.Start();
    }

    public event EventHandler<IntEventArgs> CurrentTurnNumberChanged;
    protected virtual void FireCurrentTurnNumberChanged(IntEventArgs e)
    {
      CurrentTurnNumberChanged?.Invoke(this, e);
    }
    public event EventHandler<DateTimeEventArgs> HostingTimeChanged;
    protected virtual void FireHostingTimeChanged(DateTimeEventArgs e)
    {
      HostingTimeChanged?.Invoke(this, e);
    }

    public event EventHandler<CollectionChangeEventArgs> RaceStatusChanged;
    protected virtual void FireRaceStatusChanged(CollectionChangeEventArgs e)
    {
      RaceStatusChanged?.Invoke(this, e);
    }

    bool performingUpdate = false;
    internal async void Update()
    {
      if (!performingUpdate)
      {
        performingUpdate = true;

        await Task.Run(() =>
        {
          string data = GetServerData(this.gameName);

          // Find the remaining time in the string
          {
            DateTime result;
            if (ExtractHostingTime(data, out result))
            {
              FireHostingTimeChanged(new DateTimeEventArgs(result));
            }
          }

          // Find the current turn number
          {
            int result;
            if (ExtractTurnNumber(data, out result))
            {
              FireCurrentTurnNumberChanged(new IntEventArgs(result));
            }
          }

          // Find the state of each races turn
          {
            Dictionary<string, bool> result = ExtractRaceInfo(data);
            FireRaceStatusChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, result));
          }

          performingUpdate = false;
        });
      }
    }

    internal static string GetServerData(string gameName)
    {
      string data = "";
      try
      {
        string urlAddress = "http://www.llamaserver.net/gameinfo.cgi?game=" + gameName;

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

        if (response.StatusCode == HttpStatusCode.OK)
        {
          Stream receiveStream = response.GetResponseStream();
          StreamReader readStream = null;

          if (response.CharacterSet == null)
          {
            readStream = new StreamReader(receiveStream);
          }
          else
          {
            readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
          }

          data = readStream.ReadToEnd();

          response.Close();
          readStream.Close();
        }
      }
      catch (Exception)
      {
        // Likely no internet connection
      }

      return data;
    }

    internal static bool ExtractHostingTime(string data, out DateTime result)
    {
      bool success = false;
      result = new DateTime();
      string pattern = @"Next turn due: (.*)\n";
      Regex re = new Regex(pattern);
      MatchCollection matches = re.Matches(data);
      if (matches.Count == 1)
      {
        if (matches[0].Captures.Count == 1)
        {
          if (matches[0].Groups.Count == 2)
          {
            string s = matches[0].Groups[1].Value;
            // trim the trainling 'st' 'nd' 'rd' 'th' from the string
            s = s.Remove(s.Length - 2);
            success = DateTime.TryParseExact(s,
              "HH:mm GMT on dddd MMMM d",
              new System.Globalization.CultureInfo("en-US"),
              System.Globalization.DateTimeStyles.None,
              out result);
          }
        }
      }
      return success;
    }

    internal static bool ExtractTurnNumber(string data, out int result)
    {
      bool success = false;
      result = -1;
      string pattern = @"Turn number (\d*)";
      Regex re = new Regex(pattern);
      MatchCollection matches = re.Matches(data);
      if (matches.Count == 1)
      {
        if (matches[0].Captures.Count == 1)
        {
          if (matches[0].Groups.Count == 2)
          {
            string s = matches[0].Groups[1].Value;
            result = int.Parse(s);
            success = true;
          }
        }
      }
      return success;
    }

    internal static Dictionary<string, bool> ExtractRaceInfo(string data)
    {
      Dictionary<string, bool> result = new Dictionary<string, bool>();
      string pattern = @"<tr><td>(.*)<\/td><td>&nbsp;&nbsp;&nbsp;&nbsp;<\/td><td>(2h file received|Waiting for 2h file)<\/td><\/tr>\n";
      Regex re = new Regex(pattern);
      MatchCollection matches = re.Matches(data);
      foreach (Match m in matches)
      {
        if (m.Captures.Count == 1 && m.Groups.Count == 3)
        {
          bool turnComplete = false;
          if (m.Groups[2].Value == "2h file received")
            turnComplete = true;
          string raceName = m.Groups[1].Value.Trim(' ');
          result[raceName] = turnComplete;
        }
      }

      return result;
    }
  }
}
