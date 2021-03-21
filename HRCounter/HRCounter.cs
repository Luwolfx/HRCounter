﻿using System.Collections;
using CountersPlus.Counters.Custom;
using HRCounter.Configuration;
using HRCounter.Data;
using TMPro;
using IPALogger = IPA.Logging.Logger;
using UnityEngine;
using System;


namespace HRCounter
{
    public class HRCounter: BasicCustomCounter
    {

        private readonly string URL = PluginConfig.Instance.Feed_link;
        private IPALogger log = Logger.logger;
        private TMP_Text counter;
        private bool updating;
        private BpmDownloader _bpmDownloader = new BpmDownloader();

        // color stuff
        private bool _colorize = PluginConfig.Instance.Colorize;
        private int _hrLow = PluginConfig.Instance.HRLow;
        private int _hrHigh = PluginConfig.Instance.HRHigh;
        private string _colorLow = PluginConfig.Instance.LowColor;
        private string _colorHigh = PluginConfig.Instance.HighColor;
        

        public override void CounterInit()
        {
            if (URL == "NotSet")
            {
                log.Debug("Feed link not set.");
                return;
            }
            
            log.Debug("Creating counter");
            counter = CanvasUtility.CreateTextFromSettings(Settings);
            counter.fontSize = 3;
            log.Debug("Starts updating");
            _bpmDownloader.updating = true;
            SharedCoroutineStarter.instance.StartCoroutine(_bpmDownloader.Updating());
            updating = true;
            SharedCoroutineStarter.instance.StartCoroutine(Ticking());
        }

        private IEnumerator Ticking()
        {
            while(updating)
            {
                string bpm = _bpmDownloader.bpm.Bpm;
                if (_colorize)
                {
                    if (Int32.TryParse(bpm, out int bpmInt))
                    {
                        counter.text = $"<color=#FFFFFF>HR </color><color=#{DetermineColor(bpmInt)}>{bpm}</color>";
                    }
                }
                else
                {
                    counter.text = $"HR {bpm}";
                }

                yield return new WaitForSecondsRealtime(1);
            }
        }

        private string DetermineColor(int hr)
        {
            if (_hrHigh > _hrLow && _hrLow > 0)
            {
                if (ColorUtility.TryParseHtmlString(_colorHigh, out Color colorHigh) &&
                    ColorUtility.TryParseHtmlString(_colorLow, out Color colorLow))
                {
                    if (hr <= _hrLow)
                    {
                        return _colorLow.Substring(1); //the rgb color in setting are #RRGGBB, need to omit the #
                    }

                    if (hr >= _hrHigh)
                    {
                        return _colorHigh.Substring(1);
                    }
                    Color color = Color.Lerp(colorLow, colorHigh, (hr - _hrLow) / (float) (_hrHigh - _hrLow));
                    return ColorUtility.ToHtmlStringRGB(color);
                }
            }
            log.Warn("Cannot determine color, please check hr boundaries and color codes.");
            return ColorUtility.ToHtmlStringRGB(Color.white);
        }

        public override void CounterDestroy()
        {
            updating = false;
            _bpmDownloader.updating = false;
            log.Debug("Counter destroyed");
        }
    }
}