using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcTenantDetails
	{
		[JsonPropertyName("id")]
		[JsonConverter(typeof(JsonAutoLongConverter))]
		public long Id { get; set; }

		[JsonPropertyName("email")]
		public string Email1 { get; set; } = string.Empty;

		[JsonPropertyName("email_2")]
		public string? Email2 { get; set; }

		[JsonPropertyName("email_3")]
		public string? Email3 { get; set; }

		public string?[] ValidEmails
		{
			get
			{
				IEnumerable<string?> GetEmails()
				{
					if (IsValidEmail(Email1, out var email1)) yield return email1;
					if (IsValidEmail(Email2, out var email2)) yield return email2;
					if (IsValidEmail(Email3, out var email3)) yield return email3;
				}

				return GetEmails().ToArray();
			}
		}

		public string Emails => string.Join("|", ValidEmails);

		[JsonPropertyName("phone")]
		public string Phone1 { get; set; } = string.Empty;

		[JsonPropertyName("phone_2")]
		public string? Phone2 { get; set; }

		[JsonPropertyName("phone_3")]
		public string? Phone3 { get; set; }

		public string[] ValidPhones
		{
			get
			{
				IEnumerable<string> GetPhones()
				{
					if (IsValidPhone(Phone1, out var phone1)) yield return phone1!;
					if (IsValidPhone(Phone2, out var phone2)) yield return phone2!;
					if (IsValidPhone(Phone3, out var phone3)) yield return phone3!;
				}

				return GetPhones().ToArray();
			}
		}

		public string Phones => string.Join("|", ValidPhones);

		public string FirstName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("status")]
		public TcTenantStatus Status { get; set; }


		private static readonly Regex EmailRegex = new Regex(
			@"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])",
			RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private bool IsValidEmail(string? s, out string? output)
		{
			output = null;

			if (string.IsNullOrWhiteSpace(s))
			{
				return false;
			}

			var match = EmailRegex.Match(s);

			if (match.Success)
			{
				output = match.Value;
				return true;
			}

			return false;
		}

		private static readonly Regex PhoneRegex = new Regex(
			@"(\+)?(\d[\-\s\(\)]?){8,15}",
			RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

		private bool IsValidPhone(string? s, out string? output)
		{
			output = null;

			if (string.IsNullOrWhiteSpace(s))
			{
				return false;
			}

			var match = PhoneRegex.Match(s);

			if (match.Success)
			{
				var chars = new List<char>(match.Groups.Count);
				foreach (var c in s)
				{
					if (char.IsDigit(c))
					{
						chars.Add(c);
					}
				}

				output = new string(chars.ToArray());

				return true;
			}

			return false;
		}
	}

	public enum TcTenantStatus: byte
	{

	}
}
