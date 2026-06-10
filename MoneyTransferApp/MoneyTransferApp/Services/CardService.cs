using System;
using System.Collections.Generic;
using System.Linq;
using MoneyTransferApp.Models;

namespace MoneyTransferApp.Services
{
    public class CardService
    {
        private static CardService _instance;
        private static readonly object _lock = new object();
        private static List<Card> _cards = new List<Card>();

        private CardService() { }

        public static CardService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new CardService();
                    }
                    return _instance;
                }
            }
        }

        // Генерация номера карты (16 цифр)
        private string GenerateCardNumber()
        {
            Random random = new Random();
            string bin = "4276"; // Банковский идентификатор (Mastercard)
            string account = "";
            for (int i = 0; i < 12; i++)
            {
                account += random.Next(0, 10).ToString();
            }
            return bin + account;
        }

        // Расчёт контрольной цифры (Luhn algorithm)
        private string CalculateLuhnDigit(string number)
        {
            int sum = 0;
            bool alternate = false;
            for (int i = number.Length - 1; i >= 0; i--)
            {
                int digit = int.Parse(number[i].ToString());
                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }
                sum += digit;
                alternate = !alternate;
            }
            int checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit.ToString();
        }

        // Генерация полного номера карты
        public string GenerateFullCardNumber()
        {
            string partialNumber = GenerateCardNumber();
            string checkDigit = CalculateLuhnDigit(partialNumber);
            return partialNumber + checkDigit;
        }

        // Генерация срока действия (через 4 года)
        private string GenerateExpiryDate()
        {
            var expiry = DateTime.Now.AddYears(4);
            return expiry.ToString("MM/yy");
        }

        // Генерация CVV (3 цифры)
        private string GenerateCvv()
        {
            Random random = new Random();
            return random.Next(100, 999).ToString();
        }

        // Создание карты для пользователя
        public Card CreateCardForUser(string userId)
        {
            string cardNumber = GenerateFullCardNumber();
            string maskedNumber = "**** **** **** " + cardNumber.Substring(cardNumber.Length - 4);

            var card = new Card
            {
                UserId = userId,
                CardNumber = cardNumber,
                MaskedNumber = maskedNumber,
                ExpiryDate = GenerateExpiryDate(),
                Cvv = GenerateCvv(),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _cards.Add(card);
            return card;
        }

        // Получение карты пользователя
        public Card GetCardByUserId(string userId)
        {
            return _cards.FirstOrDefault(c => c.UserId == userId && c.IsActive);
        }

        // Получение всех карт пользователя
        public List<Card> GetCardsByUserId(string userId)
        {
            return _cards.Where(c => c.UserId == userId).ToList();
        }

        // Блокировка карты
        public bool BlockCard(string cardId)
        {
            var card = _cards.FirstOrDefault(c => c.Id == cardId);
            if (card == null) return false;
            card.IsActive = false;
            return true;
        }
    }
}