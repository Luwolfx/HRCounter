﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HRCounter.Configuration;
using IPA.Utilities;
using IPA.Utilities.Async;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using UObject = UnityEngine.Object;

namespace HRCounter;

internal class IconManager: IInitializable, IDisposable
{
    [Inject] private readonly PluginConfig _config = null!;

    [Inject] private readonly SiraLog _logger = null!;
    
    private readonly DirectoryInfo _iconDir = new DirectoryInfo(Path.Combine(UnityGame.UserDataPath, "HRCounter", "Icons"));
    
    private readonly Dictionary<string, Sprite> _loadedIcons = new Dictionary<string, Sprite>();
    
    private readonly HashSet<string> _acceptableExtensions = [".png", ".jpg", ".jpeg", ".gif"];

    internal string IconDirPath => _iconDir.FullName;
    
    void IInitializable.Initialize()
    {
        _iconDir.Create();
        UnityMainThreadTaskScheduler.Factory.StartNew(LoadAllIconsAsync);
    }
    
    void IDisposable.Dispose()
    {
        ClearIconCache();
    }

    internal bool TryGetIconSprite(string name, out Sprite sprite)
    {
        return _loadedIcons.TryGetValue(name, out sprite);
    }
    
    private void ClearIconCache()
    {
        foreach (var icon in _loadedIcons)
        {
            UObject.Destroy(icon.Value);
        }
        
        _loadedIcons.Clear();
    }
    
    internal async Task<IList<Tuple<string, Sprite>>> GetIconsWithSpriteAsync(bool refresh)
    {
        if (!UnityGame.OnMainThread)
            throw new InvalidOperationException("This method can only be called from the main thread.");

        if (refresh)
        {
            await LoadAllIconsAsync();
        }

        return _loadedIcons.Select(icon => new Tuple<string, Sprite>(icon.Key, icon.Value)).ToList();
    }

    private async Task LoadAllIconsAsync()
    {
        _logger.Debug("Loading all icons");
        ClearIconCache();
        foreach (var file in _iconDir.EnumerateFiles())
        {
            if (file.Exists && _acceptableExtensions.Contains(file.Extension))
            {
                var name = file.Name;
                var sprite = await LoadIconSpriteAsync(file);
                if (sprite != null && !string.IsNullOrWhiteSpace(name))
                {
                    _loadedIcons[name] = sprite; 
                }
            }
        }
        
        _logger.Debug($"Loaded {_loadedIcons.Count} icons");
    }

    private async Task<Sprite?> LoadIconSpriteAsync(FileInfo file)
    {
        var fileName = file.Name;
        _logger.Trace($"Loading icon {fileName}");
        
        try
        {
            _logger.Trace($"Creating texture from {fileName}");
            // use this so I don't have to read the file into a byte[]
            var texture = await BeatSaberMarkupLanguage.Utilities.LoadImageAsync(file.FullName);
            if (texture == null)
            {
                _logger.Warn($"Failed to load image from {fileName}");
                return null;
            }
        
            texture.name = fileName;
            var sprite = BeatSaberMarkupLanguage.Utilities.LoadSpriteFromTexture(texture);

            _logger.Trace($"Loaded sprite from {fileName}");
            return sprite;
        }
        catch (Exception e)
        {
            _logger.Critical($"Failed to load icon sprite from {fileName}");
            _logger.Critical(e);
            return null;
        }
    }
}