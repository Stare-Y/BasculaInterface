﻿using Core.Application.Services;
using Core.Application.Settings;
using Core.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Timers;

namespace Infrastructure.Service
{
    public class BasculaService : IBasculaService
    {
        private SerialPort _bascula = null!;

        private System.Timers.Timer _pollingTimer = null!;

        private int _lecturasFalsas = 0;

        private readonly string _askChar = "P";

        private readonly ILogger<BasculaService> _logger;

        public event EventHandler<OnBasculaReadEventArgs>? OnBasculaRead;

        private readonly WeightSettings _printSettings;

        public BasculaService(IOptions<WeightSettings> optionsSettings, ILogger<BasculaService> logger)
        {
            _printSettings = optionsSettings.Value;

            _bascula = new SerialPort(_printSettings.BasculaPort, _printSettings.BasculaBauds, Parity.None, 8, StopBits.One);

            _bascula.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            if (_printSettings.NeedManualAsk)
            {
                _pollingTimer = new(750);
                _pollingTimer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
                _pollingTimer.AutoReset = true;
                _pollingTimer.Start();
                _askChar = _printSettings.AskChar;
            }

            _bascula.Open();

            _logger = logger;
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (_bascula.IsOpen)
                {
                    _bascula.Write(_askChar);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar comando a la báscula");
                Console.WriteLine($"Error al enviar comando: {ex.Message}");
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string readData = sp.ReadExisting();

            double currentWeight = ParseScreenWeight(readData);

            if (currentWeight > -1)
            {
                OnBasculaRead?.Invoke(this, new OnBasculaReadEventArgs(currentWeight));
            }
        }

        private double ParseScreenWeight(string value)
        {
            value = value.Replace(" ", "").Trim();
            var match = Regex.Match(value, @"-?\d+(\.\d+)?");

            if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                _lecturasFalsas = 0;
                return result;
            }
            else
            {
                if (++_lecturasFalsas > 5)
                {
                    _logger.LogWarning("Se han recibido más de 5 lecturas inválidas consecutivas. Reiniciando contador.");
                    _lecturasFalsas = 0;
                    return 0;
                }
            }

            return -1;
        }
    }
}
