using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Bsa.Msa.Common.Helpers
{
	/// <summary>
	/// The cryptography extensions.
	/// </summary>
	public static class CryptographyExtensions
	{
		private static readonly byte[] EmptyHashBytes;
		private static readonly string EmptyHashString;
		private static readonly long EmptyHashLong;

		static CryptographyExtensions()
		{
			using (var md5 = MD5.Create())
			{
				EmptyHashBytes = md5.ComputeHash(Encoding.Unicode.GetBytes(string.Empty));
			}

			EmptyHashString = EmptyHashBytes.ToStr();
			EmptyHashLong = EmptyHashBytes.ToLong();
		}


		/// <summary>
		/// Computes MD5 hash.
		/// </summary>
		/// <param name="source">The input string to compute.</param>
		/// <returns>The computed hash.</returns>
		public static byte[] ComputeMd5Hash(this string source)
		{
			if (string.IsNullOrEmpty(source))
				return EmptyHashBytes;

			using (var md5 = MD5.Create())
			{
				return md5.ComputeHash(Encoding.Unicode.GetBytes(source));
			}
		}

		public static long ComputeMd5Long(this string source)
		{
			if (string.IsNullOrEmpty(source))
				return EmptyHashLong;

			return source.ComputeMd5Hash().ToLong();
		}

		public static string ComputeMd5String(this string source)
		{
			if (string.IsNullOrEmpty(source))
				return EmptyHashString;

			return source.ComputeMd5Hash().ToStr();
		}

		public static string ComputeMd5StringWithoutRetweet(this string source)
		{
			if (string.IsNullOrEmpty(source))
				return EmptyHashString;

			var reg = "RT\x20@.*?:";
			var cleanStr = Regex.Replace(source, reg, "", RegexOptions.Compiled);
			return cleanStr.ComputeMd5String();
		}

		public static long ToLong(this byte[] source)
		{
			return BitConverter.ToInt64(source,0);
		}

		public static string ToStr(this byte[] source)
		{
			var sBuilder = new StringBuilder();
			foreach (byte t in source)
			{
				sBuilder.Append(t.ToString("x2"));
			}
			return sBuilder.ToString();
		}

		/// <summary>
		/// Converts the byte array to GUID.
		/// </summary>
		/// <param name="source">The byte array to convert.</param>
		/// <returns>The converted GUID.</returns>
		public static Guid ToGuid(this byte[] source)
		{
			return new Guid(source);
		}

		/// <summary>
		/// Computes the GUID from input string using MD5 hash.
		/// </summary>
		/// <param name="source">The input string to compute.</param>
		/// <returns>The computed GUID.</returns>
		public static Guid ComputeId(this string source)
		{
			return source.ComputeMd5Hash().ToGuid();
		}
	}
}
