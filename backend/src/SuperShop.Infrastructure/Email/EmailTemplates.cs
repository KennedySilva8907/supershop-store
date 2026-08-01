using System.Globalization;
using System.Net;
using System.Text;

namespace SuperShop.Infrastructure.Email;

public record EmailBody(string Html, string Text);

public record OrderLineSummary(string ProductName, string SizeLabel, int Quantity, decimal LineTotal);

public record OrderEmailModel(
    string OrderNumber,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    string ShippingFullName,
    string ShippingLine1,
    string? ShippingLine2,
    string ShippingPostalCode,
    string ShippingCity,
    IReadOnlyList<OrderLineSummary> Lines,
    string PaymentLabel,
    string OrderUrl);

public static class EmailTemplates
{
    private const string Ink = "#0a0a0a";
    private const string Bg = "#faf8f6";
    private const string Muted = "#6b6560";
    private const string Line = "#ddd7d0";
    private const string Accent = "#e8ff3a";

    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-PT");

    public static EmailBody AccountConfirmation(string name, string url) => Compose(
        headline: "Falta<br>um passo.",
        intro: $"Olá {Escape(name)}. Confirma o teu email e a conta fica pronta para comprares.",
        buttonLabel: "Confirmar conta",
        buttonUrl: url,
        smallPrint: "Não foste tu? Ignora esta mensagem.",
        extra: null,
        textBody: $"Ola {name},\n\nConfirma o teu email para comecares a comprar na SuperShop:\n{url}\n\nSe nao foste tu a criar esta conta, ignora esta mensagem.");

    public static EmailBody PasswordReset(string name, string url) => Compose(
        headline: "Nova<br>password.",
        intro: $"Olá {Escape(name)}. Pediste para definir uma nova password. O link é válido durante uma hora.",
        buttonLabel: "Definir password",
        buttonUrl: url,
        smallPrint: "Se não foste tu, ignora esta mensagem. A password atual continua válida.",
        extra: null,
        textBody: $"Ola {name},\n\nPediste para definir uma nova password. O link e valido durante uma hora:\n{url}\n\nSe nao foste tu, ignora esta mensagem. A password atual continua valida.");

    public static EmailBody OrderConfirmation(string name, OrderEmailModel order)
    {
        var rows = new StringBuilder();

        foreach (var line in order.Lines)
        {
            rows.Append($"""
                <tr>
                  <td style="padding:10px 0;border-bottom:1px solid {Line};font-family:Arial,sans-serif;font-size:14px;color:{Ink};">
                    {Escape(line.ProductName)}<br>
                    <span style="font-size:12px;color:{Muted};">Tamanho {Escape(line.SizeLabel)} &nbsp;·&nbsp; {line.Quantity} un.</span>
                  </td>
                  <td align="right" style="padding:10px 0;border-bottom:1px solid {Line};font-family:'Courier New',monospace;font-size:14px;color:{Ink};white-space:nowrap;">{Money(line.LineTotal)}</td>
                </tr>
                """);
        }

        var extra = $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
              {rows}
              <tr>
                <td style="padding:12px 0 0;font-family:Arial,sans-serif;font-size:13px;color:{Muted};">Subtotal</td>
                <td align="right" style="padding:12px 0 0;font-family:'Courier New',monospace;font-size:13px;color:{Muted};">{Money(order.Subtotal)}</td>
              </tr>
              <tr>
                <td style="padding:4px 0 0;font-family:Arial,sans-serif;font-size:13px;color:{Muted};">Portes</td>
                <td align="right" style="padding:4px 0 0;font-family:'Courier New',monospace;font-size:13px;color:{Muted};">{(order.ShippingCost == 0 ? "Grátis" : Money(order.ShippingCost))}</td>
              </tr>
              <tr>
                <td style="padding:12px 0 0;border-top:1px solid {Line};font-family:Arial,sans-serif;font-size:15px;color:{Ink};">Total</td>
                <td align="right" style="padding:12px 0 0;border-top:1px solid {Line};font-family:'Courier New',monospace;font-size:15px;color:{Ink};">{Money(order.Total)}</td>
              </tr>
            </table>

            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-top:24px;">
              <tr><td style="font-family:'Courier New',monospace;font-size:10px;letter-spacing:.2em;color:{Muted};">ENVIO</td></tr>
              <tr><td style="padding-top:8px;font-family:Arial,sans-serif;font-size:14px;color:{Ink};">{Escape(order.ShippingFullName)}</td></tr>
              <tr><td style="font-family:Arial,sans-serif;font-size:14px;color:{Muted};">{Escape(order.ShippingLine1)}{(string.IsNullOrWhiteSpace(order.ShippingLine2) ? "" : ", " + Escape(order.ShippingLine2!))}</td></tr>
              <tr><td style="font-family:'Courier New',monospace;font-size:12px;color:{Muted};">{Escape(order.ShippingPostalCode)} {Escape(order.ShippingCity)}</td></tr>
              <tr><td style="padding-top:14px;font-family:'Courier New',monospace;font-size:10px;letter-spacing:.2em;color:{Muted};">PAGAMENTO</td></tr>
              <tr><td style="padding-top:6px;font-family:Arial,sans-serif;font-size:14px;color:{Ink};">{Escape(order.PaymentLabel)}</td></tr>
            </table>
            """;

        var text = new StringBuilder();
        text.Append($"Ola {name},\n\nRecebemos a tua encomenda {order.OrderNumber}.\n\n");

        foreach (var line in order.Lines)
        {
            text.Append($"- {line.ProductName} | tamanho {line.SizeLabel} | {line.Quantity} un. | {Money(line.LineTotal)}\n");
        }

        text.Append($"\nSubtotal: {Money(order.Subtotal)}\n");
        text.Append($"Portes: {(order.ShippingCost == 0 ? "Gratis" : Money(order.ShippingCost))}\n");
        text.Append($"Total: {Money(order.Total)}\n\n");
        text.Append($"Envio para {order.ShippingFullName}, {order.ShippingLine1}, {order.ShippingPostalCode} {order.ShippingCity}\n");
        text.Append($"Pagamento: {order.PaymentLabel}\n\n");
        text.Append($"Acompanha a encomenda em {order.OrderUrl}\n");

        return Compose(
            headline: "Encomenda<br>recebida.",
            intro: $"Olá {Escape(name)}. A tua encomenda <span style=\"font-family:'Courier New',monospace;\">{Escape(order.OrderNumber)}</span> foi registada.",
            buttonLabel: "Ver encomenda",
            buttonUrl: order.OrderUrl,
            smallPrint: "Avisamos-te assim que seguir para entrega.",
            extra: extra,
            textBody: text.ToString());
    }

    private static EmailBody Compose(
        string headline,
        string intro,
        string buttonLabel,
        string buttonUrl,
        string smallPrint,
        string? extra,
        string textBody)
    {
        var html = $"""
            <!doctype html>
            <html lang="pt">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background:#e8e6e3;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#e8e6e3;">
                <tr><td align="center" style="padding:28px 16px;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background:{Bg};">

                    <tr><td style="background:{Ink};padding:26px 30px;">
                      <div style="font-family:Arial,Helvetica,sans-serif;font-weight:bold;font-size:22px;letter-spacing:.2em;color:{Bg};">SUPERSHOP</div>
                      <div style="margin-top:6px;font-family:'Courier New',monospace;font-size:10px;letter-spacing:.24em;color:{Accent};">AXIS &nbsp;·&nbsp; CORE</div>
                    </td></tr>

                    <tr><td style="padding:30px 30px 0;">
                      <div style="font-family:Arial,Helvetica,sans-serif;font-size:30px;font-weight:bold;line-height:1.12;color:{Ink};text-transform:uppercase;">{headline}</div>
                      <p style="margin:16px 0 0;font-family:Arial,Helvetica,sans-serif;font-size:14px;line-height:1.65;color:{Muted};">{intro}</p>
                    </td></tr>

                    <tr><td style="padding:24px 30px 0;">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr>
                        <td align="center" style="background:{Ink};">
                          <a href="{buttonUrl}" style="display:block;padding:16px;font-family:Arial,Helvetica,sans-serif;font-size:14px;color:{Bg};text-decoration:none;">{buttonLabel}</a>
                        </td>
                      </tr></table>
                    </td></tr>

                    {(extra is null ? "" : $"<tr><td style=\"padding:28px 30px 0;\">{extra}</td></tr>")}

                    <tr><td style="padding:20px 30px 0;">
                      <p style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:1.6;color:{Muted};">{smallPrint}</p>
                    </td></tr>

                    <tr><td style="padding:26px 30px 30px;">
                      <table role="presentation" cellpadding="0" cellspacing="0"><tr>
                        <td style="background:{Accent};padding:7px 14px;font-family:'Courier New',monospace;font-size:10px;letter-spacing:.2em;color:{Ink};">STREETWEAR FEITO PARA DURAR</td>
                      </tr></table>
                    </td></tr>

                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        return new EmailBody(html, textBody);
    }

    private static string Money(decimal value) => value.ToString("C", Pt);

    private static string Escape(string value) => WebUtility.HtmlEncode(value);
}
