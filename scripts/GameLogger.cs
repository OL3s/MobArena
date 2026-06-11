using System;
using System.IO;
using System.Runtime.CompilerServices;
using Godot;

public enum GameLogCategory
{
    Info,
    Save,
    CLI,
    SceneTransition,
    Combat,
    Contract,
    UI,
    Data,
    State,
    Input,
}

public enum GameLogLevel
{
    Info,
    Warning,
    Error,
}

public static class GameLogger
{
    public static void Print(
        GameLogCategory category,
        object message,
        GameLogLevel level = GameLogLevel.Info,
        [CallerFilePath] string callerFilePath = "")
    {
        Write(category, message, level, callerFilePath);
    }

    public static void Info(object message, [CallerFilePath] string callerFilePath = "") =>
        Write(GameLogCategory.Info, message, GameLogLevel.Info, callerFilePath);

    public static void Save(object message, [CallerFilePath] string callerFilePath = "") =>
        Write(GameLogCategory.Save, message, GameLogLevel.Info, callerFilePath);

    public static void CLI(object message, [CallerFilePath] string callerFilePath = "") =>
        Write(GameLogCategory.CLI, message, GameLogLevel.Info, callerFilePath);

    public static void SceneTransition(object message, [CallerFilePath] string callerFilePath = "") =>
        Write(GameLogCategory.SceneTransition, message, GameLogLevel.Info, callerFilePath);

    public static void Combat(object message, [CallerFilePath] string callerFilePath = "") =>
        Write(GameLogCategory.Combat, message, GameLogLevel.Info, callerFilePath);

    public static void Contract(object message, [CallerFilePath] string callerFilePath = "") =>
        Write(GameLogCategory.Contract, message, GameLogLevel.Info, callerFilePath);

    public static void UI(object message, [CallerFilePath] string callerFilePath = "") =>
        Write(GameLogCategory.UI, message, GameLogLevel.Info, callerFilePath);

    public static void Data(object message, [CallerFilePath] string callerFilePath = "") =>
        Write(GameLogCategory.Data, message, GameLogLevel.Info, callerFilePath);

    public static void State(object message, [CallerFilePath] string callerFilePath = "") =>
        Write(GameLogCategory.State, message, GameLogLevel.Info, callerFilePath);

    public static void Input(object message, [CallerFilePath] string callerFilePath = "") =>
        Write(GameLogCategory.Input, message, GameLogLevel.Info, callerFilePath);

    public static void Warning(GameLogCategory category, object message, [CallerFilePath] string callerFilePath = "") =>
        Write(category, message, GameLogLevel.Warning, callerFilePath);

    public static void Error(GameLogCategory category, object message, [CallerFilePath] string callerFilePath = "") =>
        Write(category, message, GameLogLevel.Error, callerFilePath);

    private static void Write(GameLogCategory category, object message, GameLogLevel level, string callerFilePath)
    {
        string formattedMessage = Format(category, message, level, callerFilePath);
        switch (level)
        {
            case GameLogLevel.Warning:
                GD.PushWarning(formattedMessage);
                break;
            case GameLogLevel.Error:
                GD.PushError(formattedMessage);
                break;
            default:
                GD.Print(formattedMessage);
                break;
        }
    }

    private static string Format(GameLogCategory category, object message, GameLogLevel level, string callerFilePath)
    {
        string source = string.IsNullOrWhiteSpace(callerFilePath) ? "Unknown" : Path.GetFileName(callerFilePath);
        string categoryName = FormatCategory(category);
        string text = StripMatchingSourcePrefix(message, source);

        return level == GameLogLevel.Info
            ? $"[{source}][{categoryName}]: {text}"
            : $"[{source}][{categoryName}][{level}]: {text}";
    }

    private static string FormatCategory(GameLogCategory category)
    {
        return category switch
        {
            GameLogCategory.CLI => "CLI",
            GameLogCategory.SceneTransition => "Scene Transition",
            GameLogCategory.UI => "UI",
            _ => category.ToString(),
        };
    }

    private static string StripMatchingSourcePrefix(object message, string source)
    {
        string text = message?.ToString() ?? "null";
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(source))
        {
            return text;
        }

        string sourceBase = Path.GetFileNameWithoutExtension(source);
        string prefix = sourceBase + ":";
        return text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? text[prefix.Length..].TrimStart()
            : text;
    }
}
