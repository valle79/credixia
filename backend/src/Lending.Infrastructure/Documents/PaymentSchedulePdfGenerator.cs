
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lending.Infrastructure.Documents;

public sealed class PaymentSchedulePdfGenerator : IPaymentSchedulePdfGenerator
{
    private static readonly CultureInfo Culture =
        CultureInfo.GetCultureInfo("es-PE");

    public async Task<byte[]> GeneratePdfAsync(
        PaymentScheduleData data,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        QuestPDF.Settings.License = LicenseType.Community;

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var document = Document.Create(documentContainer =>
            {
                documentContainer.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.2f, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    page.DefaultTextStyle(style => style
                        .FontFamily("Lato")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken4));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
            });

            cancellationToken.ThrowIfCancellationRequested();

            return document.GeneratePdf();

            // ================================================
            // HEADER
            // ================================================

            void ComposeHeader(IContainer container)
            {
                container.Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item()
                                .Text(data.OrganizationName)
                                .FontSize(20)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);

                            col.Item()
                                .PaddingTop(3)
                                .Text("CRONOGRAMA DE PAGOS")
                                .FontSize(10)
                                .SemiBold()
                                .FontColor(Colors.Teal.Darken2);
                        });

                        row.ConstantItem(190)
                            .AlignRight()
                            .Column(col =>
                            {
                                col.Item()
                                    .AlignRight()
                                    .Text("NÚMERO DE PRÉSTAMO")
                                    .FontSize(7)
                                    .FontColor(Colors.Grey.Darken1);

                                col.Item()
                                    .PaddingTop(3)
                                    .AlignRight()
                                    .Text(data.LoanNumber)
                                    .FontSize(13)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken3);

                                col.Item()
                                    .PaddingTop(4)
                                    .AlignRight()
                                    .Text($"Emitido: {DateTime.Now:dd/MM/yyyy}")
                                    .FontSize(7.5f)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                    });

                    column.Item()
                        .PaddingTop(10)
                        .Height(2)
                        .Background(Colors.Teal.Darken2);
                });
            }

            // ================================================
            // CONTENT
            // ================================================

            void ComposeContent(IContainer container)
            {
                container.PaddingVertical(8).Column(column =>
                {
                    // CLIENTE
                    SectionTitle(column, "INFORMACIÓN DEL CLIENTE");

                    column.Item()
                        .PaddingTop(5)
                        .Background(Colors.Grey.Lighten4)
                        .Padding(9)
                        .Row(row =>
                        {
                            row.RelativeItem(2).Column(col =>
                            {
                                Label(col, "CLIENTE");
                                Value(col, data.CustomerName);
                            });

                            row.RelativeItem().Column(col =>
                            {
                                Label(col, "DOCUMENTO");
                                Value(col, data.CustomerDocument);
                            });

                            row.RelativeItem().Column(col =>
                            {
                                Label(col, "TELÉFONO");
                                Value(
                                    col,
                                    string.IsNullOrWhiteSpace(data.CustomerPhone)
                                        ? "No registrado"
                                        : data.CustomerPhone);
                            });
                        });

                    // DETALLES DEL PRÉSTAMO
                    SectionTitle(column, "DETALLES DEL PRÉSTAMO");

                    column.Item()
                        .PaddingTop(5)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(c =>
                                SummaryCell(c, "PRODUCTO", data.ProductName));

                            table.Cell().Element(c =>
                                SummaryCell(c, "FRECUENCIA", FrequencyLabel(data.PaymentFrequency)));

                            table.Cell().Element(c =>
                                SummaryCell(c, "TASA DE INTERÉS", $"{data.InterestRate:N2}%"));

                            table.Cell().Element(c =>
                                SummaryCell(c, "TIPO DE INTERÉS", InterestTypeLabel(data.InterestType)));

                            table.Cell().Element(c =>
                                SummaryCell(c, "PLAZO", $"{data.TermDays} días"));

                            table.Cell().Element(c =>
                                SummaryCell(c, "NÚMERO DE CUOTAS", data.NumberOfPayments.ToString(CultureInfo.InvariantCulture)));

                            table.Cell().Element(c =>
                                SummaryCell(c, "DESEMBOLSO", data.DisbursementDate.ToString("dd/MM/yyyy")));

                            table.Cell().Element(c =>
                                SummaryCell(c, "PRIMER VENCIMIENTO", data.FirstPaymentDate.ToString("dd/MM/yyyy")));

                            table.Cell().ColumnSpan(4).Element(c =>
                                SummaryCell(c, "ESTADO DEL PRÉSTAMO", LoanStatusLabel(data.LoanStatus)));
                        });

                    // RESUMEN FINANCIERO
                    SectionTitle(column, "RESUMEN FINANCIERO");

                    column.Item()
                        .PaddingTop(5)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(c =>
                                FinancialCell(c, "CAPITAL", Money(data.PrincipalAmount)));

                            table.Cell().Element(c =>
                                FinancialCell(c, "INTERESES", Money(data.TotalInterest)));

                            table.Cell().Element(c =>
                                FinancialCell(
                                    c,
                                    "COMISIÓN",
                                    data.Commission.HasValue
                                        ? Money(data.Commission.Value)
                                        : "—"));

                            table.Cell().Element(c =>
                                FinancialCell(c, "TOTAL A PAGAR", Money(data.TotalAmount)));

                            table.Cell().Element(c =>
                                FinancialCell(c, "TOTAL PAGADO", Money(data.PaidAmount)));

                            table.Cell().Element(c =>
                                FinancialCell(
                                    c,
                                    "SALDO PENDIENTE",
                                    Money(data.BalanceAmount),
                                    true));
                        });

                    // CRONOGRAMA
                    SectionTitle(column, "DETALLE DEL CRONOGRAMA DE PAGOS");

                    column.Item()
                        .PaddingTop(5)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(25);
                                columns.ConstantColumn(67);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.ConstantColumn(65);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).AlignCenter().Text("#");
                                header.Cell().Element(HeaderCell).Text("VENCIMIENTO");
                                header.Cell().Element(HeaderCell).AlignRight().Text("CAPITAL");
                                header.Cell().Element(HeaderCell).AlignRight().Text("INTERÉS");
                                header.Cell().Element(HeaderCell).AlignRight().Text("CUOTA");
                                header.Cell().Element(HeaderCell).AlignRight().Text("PAGADO");
                                header.Cell().Element(HeaderCell).AlignRight().Text("PENDIENTE");
                                header.Cell().Element(HeaderCell).AlignRight().Text("SALDO");
                                header.Cell().Element(HeaderCell).AlignCenter().Text("ESTADO");
                            });

                            var index = 0;

                            foreach (var item in data.Items)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var background = item.IsCancelled
                                    ? Colors.Grey.Lighten3
                                    : index % 2 == 0
                                        ? Colors.White
                                        : Colors.Grey.Lighten4;

                                var status = item.IsCancelled
                                    ? "Cancelada"
                                    : item.IsPaid
                                        ? "Pagada"
                                        : item.IsOverdue
                                            ? "Vencida"
                                            : "Pendiente";

                                var statusColor = item.IsCancelled
                                    ? Colors.Grey.Darken1
                                    : item.IsPaid
                                        ? Colors.Green.Darken2
                                        : item.IsOverdue
                                            ? Colors.Red.Darken1
                                            : Colors.Orange.Darken2;

                                table.Cell().Element(c => DataCell(c, background))
                                    .AlignCenter()
                                    .Text(item.PaymentNumber.ToString(CultureInfo.InvariantCulture));

                                table.Cell().Element(c => DataCell(c, background))
                                    .Text(item.DueDate.ToString("dd/MM/yyyy"));

                                table.Cell().Element(c => DataCell(c, background))
                                    .AlignRight()
                                    .Text(Money(item.PrincipalAmount));

                                table.Cell().Element(c => DataCell(c, background))
                                    .AlignRight()
                                    .Text(Money(item.InterestAmount));

                                table.Cell().Element(c => DataCell(c, background))
                                    .AlignRight()
                                    .Text(Money(item.DueAmount))
                                    .SemiBold();

                                table.Cell().Element(c => DataCell(c, background))
                                    .AlignRight()
                                    .Text(Money(item.PaidAmount));

                                table.Cell().Element(c => DataCell(c, background))
                                    .AlignRight()
                                    .Text(Money(item.PendingAmount));

                                table.Cell().Element(c => DataCell(c, background))
                                    .AlignRight()
                                    .Text(Money(item.RemainingBalance));

                                table.Cell().Element(c => DataCell(c, background))
                                    .AlignCenter()
                                    .Text(status)
                                    .FontColor(statusColor)
                                    .FontSize(7)
                                    .SemiBold();

                                index++;
                            }
                        });

                    // NOTA INFORMATIVA
                    column.Item()
                        .PaddingTop(10)
                        .Background(Colors.Blue.Lighten5)
                        .BorderLeft(3)
                        .BorderColor(Colors.Teal.Darken2)
                        .Padding(8)
                        .Column(col =>
                        {
                            col.Item()
                                .Text("INFORMACIÓN IMPORTANTE")
                                .FontSize(8)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);

                            col.Item()
                                .PaddingTop(3)
                                .Text(
                                    "Este documento contiene el cronograma de pagos " +
                                    "registrado para el préstamo indicado. " +
                                    "Consérvelo como referencia para el seguimiento " +
                                    "de sus obligaciones.")
                                .FontSize(7.5f)
                                .FontColor(Colors.Grey.Darken2);
                        });
                });
            }

            // ================================================
            // FOOTER
            // ================================================

            void ComposeFooter(IContainer container)
            {
                container
                    .PaddingTop(7)
                    .BorderTop(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Text(data.OrganizationName)
                            .FontSize(7)
                            .FontColor(Colors.Grey.Darken1);

                        row.ConstantItem(120)
                            .AlignRight()
                            .Text(text =>
                            {
                                text.Span("Página ");
                                text.CurrentPageNumber();
                                text.Span(" de ");
                                text.TotalPages();
                            });
                    });
            }

            // ================================================
            // VISUAL COMPONENTS
            // ================================================

            void SectionTitle(ColumnDescriptor column, string title)
            {
                column.Item()
                    .PaddingTop(9)
                    .PaddingBottom(3)
                    .Row(row =>
                    {
                        row.ConstantItem(3)
                            .Height(12)
                            .Background(Colors.Teal.Darken2);

                        row.RelativeItem()
                            .PaddingLeft(6)
                            .AlignMiddle()
                            .Text(title)
                            .FontSize(8.5f)
                            .Bold()
                            .FontColor(Colors.Blue.Darken3);
                    });
            }

            void Label(ColumnDescriptor column, string label)
            {
                column.Item()
                    .Text(label)
                    .FontSize(7)
                    .SemiBold()
                    .FontColor(Colors.Grey.Darken1);
            }

            void Value(ColumnDescriptor column, string value)
            {
                column.Item()
                    .PaddingTop(3)
                    .Text(value)
                    .FontSize(9)
                    .SemiBold();
            }

IContainer SummaryCell(
    IContainer container,
    string label,
    string value)
{
    container
        .Border(0.5f)
        .BorderColor(Colors.Grey.Lighten2)
        .Background(Colors.White)
        .Padding(6)
        .Column(column =>
        {
            column.Item()
                .Text(label)
                .FontSize(7)
                .SemiBold()
                .FontColor(Colors.Grey.Darken1);

            column.Item()
                .PaddingTop(3)
                .Text(value)
                .FontSize(8)
                .SemiBold();
        });

    return container;
}

IContainer FinancialCell(
    IContainer container,
    string label,
    string value,
    bool highlight = false)
{
    container
        .Border(0.5f)
        .BorderColor(
            highlight
                ? Colors.Teal.Lighten2
                : Colors.Grey.Lighten2)
        .Background(
            highlight
                ? Colors.Teal.Lighten5
                : Colors.Grey.Lighten4)
        .Padding(8)
        .Column(column =>
        {
            column.Item()
                .Text(label)
                .FontSize(7)
                .SemiBold()
                .FontColor(Colors.Grey.Darken1);

            column.Item()
                .PaddingTop(4)
                .Text(value)
                .FontSize(11)
                .Bold()
                .FontColor(
                    highlight
                        ? Colors.Teal.Darken3
                        : Colors.Blue.Darken3);
        });

    return container;
}

            IContainer HeaderCell(IContainer container)
            {
                return container
                    .Background(Colors.Blue.Darken3)
                    .PaddingVertical(6)
                    .PaddingHorizontal(4)
                    .DefaultTextStyle(style => style
                        .FontColor(Colors.White)
                        .FontSize(7)
                        .SemiBold());
            }

            IContainer DataCell(IContainer container, string background)
            {
                return container
                    .BorderBottom(0.5f)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Background(background)
                    .PaddingVertical(5)
                    .PaddingHorizontal(4)
                    .DefaultTextStyle(style => style
                        .FontSize(7)
                        .FontColor(Colors.Grey.Darken3));
            }

            // ================================================
            // FORMATTERS
            // ================================================

            string Money(decimal? amount)
            {
                if (!amount.HasValue)
                    return "—";

                var currency = data.Currency?.ToUpperInvariant() switch
                {
                    "PEN" => "S/",
                    "USD" => "$",
                    "EUR" => "€",
                    var value when !string.IsNullOrWhiteSpace(value) => value,
                    _ => ""
                };

                return string.IsNullOrEmpty(currency)
                    ? amount.Value.ToString("N2", Culture)
                    : $"{currency} {amount.Value.ToString("N2", Culture)}";
            }

            string InterestTypeLabel(string value) => value switch
            {
                "Simple" => "Simple (plano)",
                "French" => "Francés (cuota fija)",
                "Compound" => "Compuesto",
                _ => value
            };

            string FrequencyLabel(string value) => value switch
            {
                "Daily" => "Diario",
                "Weekly" => "Semanal",
                "Biweekly" => "Quincenal",
                "Monthly" => "Mensual",
                "Quarterly" => "Trimestral",
                _ => value
            };

            string LoanStatusLabel(string value) => value switch
            {
                "Pending" => "Pendiente",
                "Approved" => "Aprobado",
                "Active" => "Activo",
                "PaidOff" => "Pagado",
                "Rejected" => "Rechazado",
                "Cancelled" => "Cancelado",
                "DefaultedDef" => "En mora",
                "Refinanced" => "Refinanciado",
                "SettledEarly" => "Liquidado anticipadamente",
                _ => value
            };
        }, cancellationToken);
    }
}
