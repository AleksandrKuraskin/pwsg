using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using Microsoft.Win32;

namespace DfaSimulator
{
    public static class DfaPersistence
    {
        public static void ExportJson(AutomatonData data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                var dialog = new SaveFileDialog
                {
                    Filter = "Automaton spec (*.json)|*.json",
                    FileName = "automaton.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, json);
                    MessageBox.Show("Automat zapisany pomyślnie!", "Eksport", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd zapisu: " + ex.Message, "Wystąpił błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static AutomatonData? ImportJson()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Automaton spec (*.json)|*.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    string json = File.ReadAllText(dialog.FileName);
                    var package = JsonSerializer.Deserialize<AutomatonData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (package == null || package.States == null || package.Transitions == null)
                    {
                        throw new Exception("Nieprawidłowy plik schematu DFA.");
                    }

                    return package;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd odczytu pliku: " + ex.Message, "Walidacja Schematu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        public static void SavePng(Canvas canvas)
        {
            try
            {
                int width = (int)canvas.ActualWidth;
                int height = (int)canvas.ActualHeight;
                if (width <= 0 || height <= 0) { width = 800; height = 500; }

                RenderTargetBitmap renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(canvas);

                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                var dialog = new SaveFileDialog
                {
                    Filter = "Obrazek PNG (*.png)|*.png",
                    FileName = "dfa_diagram.png"
                };

                if (dialog.ShowDialog() == true)
                {
                    using (Stream stream = File.Create(dialog.FileName))
                    {
                        encoder.Save(stream);
                    }
                    MessageBox.Show("Diagram automatu zapisany jako grafika PNG!", "Zapis PNG", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem z eksportem obrazka: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
