using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.ComponentModel;

namespace xxTalker.Shared.Models
{
    [Table("talker")]
    [Index(nameof(AccountId), IsUnique = true)]
    public class Talker
    {
        [Key]
        [Column("account_id")]
        public string AccountId { get; set; }

        [Column("info")]
        public string? Info { get; set; }

        [Column("rating")]
        public string Rating { get; set; }

        [NotMapped]
        public int RatingInt {
            get
            {
                NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
                return Convert.ToInt32(decimal.Parse(Rating, nfi));
            }
        }
        public List<TalkerMessage> Messages { get; set; }
    }

    [Table("talker_message")]
    [Index(nameof(ReceiverAccount))]
    public class TalkerMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("message_id")]
        public int MessageId { get; set; }

        [RegularExpression("^6[A-HJ-NP-Za-km-z1-9]{47}$", ErrorMessage = "ReceiverAccount length must be 48 characters, start with 6 and follow the SS58 format.")]
        [Column("receiver_account")]
        public string ReceiverAccount { get; set; }

        [RegularExpression("^6[A-HJ-NP-Za-km-z1-9]{47}$", ErrorMessage = "SenderAccount length must be 48 characters, start with 6 and follow the SS58 format.")]
        [Column("sender_account")]
        public string? SenderAccount { get; set; }

        [MaxLength(32)]
        [Column("receiver_identity")]
        public string? ReceiverIdentity { get; set; }

        [MaxLength(32)]
        [Column("sender_identity")]
        public string? SenderIdentity { get; set; }

        [MaxLength(50)]
        [Column("receiver_role")]
        public string? ReceiverRole { get; set; }

        [MaxLength(50)]
        [Column("sender_role")]
        public string? SenderRole { get; set; }

        [MaxLength(1000)]
        [Column("message")]
        public string? Message { get; set; }

        [RegularExpression("^(0x)?[0-9a-f]{128}$", ErrorMessage = "Signature length should be 128-130, 0x is optional.")]
        [Column("signature")]
        public string Signature { get; set; }

        [Column("message_num")]
        public int MessageNum { get; set; } = 0;

        [Column("message_date")]
        public DateTime MessageDate { get; set; } = DateTime.UtcNow;

        [Column("message_type")]
        public MessageType MessageType { get; set; } = MessageType.Message;

        [Column("ref_message_num")]
        public int? RefMessageNum { get; set; }

        [RegularExpression("^[0-5]{1}$", ErrorMessage = "Number from 0 to 5.")]
        [Column("rating")]
        public int Rating { get; set; } = 0;
    }

    public enum MessageType
    {
        Message,
        Rating,
        Info
    }

    public class AccountXXNetwork
    {
        public string id { get; set; }
        public string controllerAddress { get; set; }
        public bool active { get; set; }
        public long whenCreated { get; set; }
        public object whenKilled { get; set; }
        public int blockHeight { get; set; }
        public Identity? identity { get; set; }
        public int nonce { get; set; }
        public long timestamp { get; set; }
        public bool techcommit { get; set; }
        public object special { get; set; }
        public bool nominator { get; set; }
        public bool council { get; set; }
        public bool validator { get; set; }
        public string __typename { get; set; }
        public BigInteger lockedBalance { get; set; }
        public BigInteger reservedBalance { get; set; }
        public BigInteger totalBalance { get; set; }
        public BigInteger bondedBalance { get; set; }
        public BigInteger councilBalance { get; set; }
        public BigInteger democracyBalance { get; set; }
        public BigInteger transferrableBalance { get; set; }
        public BigInteger unbondingBalance { get; set; }
        public BigInteger vestingBalance { get; set; }
    }

    public class Identity
    {
        public string blurb { get; set; }
        public string display { get; set; }
        public string discord { get; set; }
        public string displayParent { get; set; }
        public string email { get; set; }
        public List<string> judgements { get; set; }
        public string legal { get; set; }
        public string riot { get; set; }
        public string twitter { get; set; }
        public bool verified { get; set; }
        public string web { get; set; }
        public string __typename { get; set; }
    }

    public class BigIntegerConverter : JsonConverter<BigInteger>
    {
        public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number)
                throw new JsonException(string.Format("Found token {0} but expected token {1}", reader.TokenType, JsonTokenType.Number));
            using var doc = JsonDocument.ParseValue(ref reader);
            return BigInteger.Parse(doc.RootElement.GetRawText(), NumberFormatInfo.InvariantInfo);
        }

        public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options) =>
            writer.WriteRawValue(value.ToString(NumberFormatInfo.InvariantInfo), false);
    }

}
