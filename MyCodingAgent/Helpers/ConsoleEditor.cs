using System.Text;

public class ConsoleEditor
{
    public static string ReadMultilineInput()
    {
        var lines = new List<StringBuilder> { new StringBuilder() };

        int cursorX = 0;
        int cursorY = 0;

        int startTop = Console.CursorTop;
        int lastRenderLineCount = 0;

        bool overwriteMode = false;

        void Render()
        {
            Console.SetCursorPosition(0, startTop);

            int lineIndex = 0;

            foreach (var l in lines)
            {
                Console.WriteLine(l.ToString().PadRight(Console.WindowWidth));
                lineIndex++;
            }

            for (int i = lineIndex; i < lastRenderLineCount; i++)
                Console.WriteLine(new string(' ', Console.WindowWidth));

            Console.WriteLine(new string(' ', Console.WindowWidth));

            lastRenderLineCount = lines.Count;

            Console.SetCursorPosition(cursorX, startTop + cursorY);
        }

        Render();

        while (true)
        {
            var key = Console.ReadKey(true);

            // SUBMIT
            if (key.Key == ConsoleKey.Enter &&
                key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                //Console.SetCursorPosition(0, startTop + lines.Count + 1);
                Console.WriteLine();
                break;
            }

            // NEWLINE
            if (key.Key == ConsoleKey.Enter)
            {
                var current = lines[cursorY];
                var newLine = new StringBuilder(current.ToString().Substring(cursorX));

                current.Length = cursorX;

                lines.Insert(cursorY + 1, newLine);

                cursorY++;
                cursorX = 0;

                Render();
                continue;
            }

            // TAB
            if (key.Key == ConsoleKey.Tab)
            {
                lines[cursorY].Insert(cursorX, "    ");
                cursorX += 4;
                Render();
                continue;
            }

            // BACKSPACE
            if (key.Key == ConsoleKey.Backspace)
            {
                if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    while (cursorX > 0 && lines[cursorY][cursorX - 1] == ' ')
                    {
                        lines[cursorY].Remove(cursorX - 1, 1);
                        cursorX--;
                    }

                    while (cursorX > 0 && lines[cursorY][cursorX - 1] != ' ')
                    {
                        lines[cursorY].Remove(cursorX - 1, 1);
                        cursorX--;
                    }
                }
                else
                {
                    if (cursorX > 0)
                    {
                        lines[cursorY].Remove(cursorX - 1, 1);
                        cursorX--;
                    }
                    else if (cursorY > 0)
                    {
                        cursorX = lines[cursorY - 1].Length;
                        lines[cursorY - 1].Append(lines[cursorY]);
                        lines.RemoveAt(cursorY);
                        cursorY--;
                    }
                }

                Render();
                continue;
            }

            // DELETE
            if (key.Key == ConsoleKey.Delete)
            {
                var line = lines[cursorY];

                if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    while (cursorX < line.Length && line[cursorX] == ' ')
                        line.Remove(cursorX, 1);

                    while (cursorX < line.Length && line[cursorX] != ' ')
                        line.Remove(cursorX, 1);
                }
                else
                {
                    if (cursorX < line.Length)
                        line.Remove(cursorX, 1);
                    else if (cursorY < lines.Count - 1)
                    {
                        line.Append(lines[cursorY + 1]);
                        lines.RemoveAt(cursorY + 1);
                    }
                }

                Render();
                continue;
            }

            // HOME
            if (key.Key == ConsoleKey.Home)
            {
                cursorX = 0;
                Render();
                continue;
            }

            // END
            if (key.Key == ConsoleKey.End)
            {
                cursorX = lines[cursorY].Length;
                Render();
                continue;
            }

            // PAGE UP
            if (key.Key == ConsoleKey.PageUp)
            {
                cursorY = 0;
                cursorX = Math.Min(cursorX, lines[cursorY].Length);
                Render();
                continue;
            }

            // PAGE DOWN
            if (key.Key == ConsoleKey.PageDown)
            {
                cursorY = lines.Count - 1;
                cursorX = Math.Min(cursorX, lines[cursorY].Length);
                Render();
                continue;
            }

            // INSERT
            if (key.Key == ConsoleKey.Insert)
            {
                overwriteMode = !overwriteMode;
                continue;
            }

            // LEFT
            if (key.Key == ConsoleKey.LeftArrow)
            {
                if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    while (cursorX > 0 && lines[cursorY][cursorX - 1] == ' ') cursorX--;
                    while (cursorX > 0 && lines[cursorY][cursorX - 1] != ' ') cursorX--;
                }
                else
                {
                    if (cursorX > 0) cursorX--;
                    else if (cursorY > 0)
                    {
                        cursorY--;
                        cursorX = lines[cursorY].Length;
                    }
                }

                Render();
                continue;
            }

            // RIGHT
            if (key.Key == ConsoleKey.RightArrow)
            {
                var line = lines[cursorY];

                if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    while (cursorX < line.Length && line[cursorX] != ' ') cursorX++;
                    while (cursorX < line.Length && line[cursorX] == ' ') cursorX++;
                }
                else
                {
                    if (cursorX < line.Length) cursorX++;
                    else if (cursorY < lines.Count - 1)
                    {
                        cursorY++;
                        cursorX = 0;
                    }
                }

                Render();
                continue;
            }

            // UP
            if (key.Key == ConsoleKey.UpArrow)
            {
                if (cursorY > 0)
                {
                    cursorY--;
                    cursorX = Math.Min(cursorX, lines[cursorY].Length);
                }

                Render();
                continue;
            }

            // DOWN
            if (key.Key == ConsoleKey.DownArrow)
            {
                if (cursorY < lines.Count - 1)
                {
                    cursorY++;
                    cursorX = Math.Min(cursorX, lines[cursorY].Length);
                }

                Render();
                continue;
            }

            // TEXT INPUT
            if (!char.IsControl(key.KeyChar))
            {
                var line = lines[cursorY];

                if (overwriteMode && cursorX < line.Length)
                    line[cursorX] = key.KeyChar;
                else
                    line.Insert(cursorX, key.KeyChar);

                cursorX++;

                Render();
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}