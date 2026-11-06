using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lending.Infrastructure.Documents;

public sealed class PaymentSchedulePdfGenerator : IPaymentSchedulePdfGenerator
{
    public async Task<byte[]> GeneratePdfAsync(PaymentScheduleData data, CancellationToken cancellationToken = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return await Task.Run(() =>
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Lato"));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();

            void ComposeHeader(IContainer container)
            {
                container.Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        if (!string.IsNullOrEmpty(data.OrganizationLogoUrl))
                        {
                            try
                            {
                                column.Item().Height(55).Image(data.OrganizationLogoUrl);
                            }
                            catch
                            {
                                column.Item().Text(data.OrganizationName)
                                    .FontSize(18)
                                    .SemiBold()
                                    .FontColor(Colors.Blue.Darken2);
                            }
                        }
                        else
                        {
                            column.Item().Text(data.OrganizationName)
                                .FontSize(18)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);
                        }

                        column.Item().PaddingTop(4).Text("Cronograma de Pagos")
                            .FontSize(15)
                            .SemiBold();
                    });

                    row.ConstantItem(150).Column(column =>
                    {
                        column.Item().AlignRight().Text($"Préstamo: {data.LoanNumber}")
                            .FontSize(10)
                            .SemiBold();
                        column.Item().AlignRight().Text($"Emitido: {DateTime.Now:dd/MM/yyyy}")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken1);
                    });
                });
            }

            void ComposeContent(IContainer container)
            {
                container.PaddingVertical(8).Column(column =>
                {
                    column.Item().Background(Colors.Grey.Lighten4)
                        .Padding(9)
                        .Row(row =>
                        {
                            row.RelativeItem(2).Column(col =>
                            {
                                col.Item().Text("Cliente").FontSize(8).FontColor(Colors.Grey.Darken1);
                                col.Item().Text(data.CustomerName).FontSize(10).SemiBold();
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Documento").FontSize(8).FontColor(Colors.Grey.Darken1);
                                col.Item().Text(data.CustomerDocument).FontSize(10).SemiBold();
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Teléfono").FontSize(8).FontColor(Colors.Grey.Darken1);
                                col.Item().Text(string.IsNullOrWhiteSpace(data.CustomerPhone) ? "—" : data.CustomerPhone)
                                    .FontSize(10)
                                    .SemiBold();
                            });
                        });

                    column.Item().PaddingTop(10).Text("Datos del Préstamo")
                        .FontSize(11)
                        .SemiBold();

                    column.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Element(c => SummaryCell(c, "Producto", data.ProductName));
                        table.Cell().Element(c => SummaryCell(c, "Frecuencia", FrequencyLabel(data.PaymentFrequency)));
                        table.Cell().Element(c => SummaryCell(c, "Tasa de interés", $"{data.InterestRate:N2}%"));

                        table.Cell().Element(c => SummaryCell(c, "Tipo de interés", InterestTypeLabel(data.InterestType)));
                        table.Cell().Element(c => SummaryCell(c, "Plazo", $"{data.TermDays} días"));
                        table.Cell().Element(c => SummaryCell(c, "N° de cuotas", data.NumberOfPayments.ToString(CultureInfo.InvariantCulture)));

                        table.Cell().Element(c => SummaryCell(c, "Fecha de desembolso", $"{data.DisbursementDate:dd/MM/yyyy}"));
                        table.Cell().Element(c => SummaryCell(c, "Primer pago", $"{data.FirstPaymentDate:dd/MM/yyyy}"));
                        table.Cell().Element(c => SummaryCell(c, "Estado", LoanStatusLabel(data.LoanStatus)));
                    });

                    column.Item().PaddingTop(10).Text("Resumen Financiero")
                        .FontSize(11)
                        .SemiBold();

                    column.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Element(c => SummaryCell(c, "Capital", Money(data.PrincipalAmount)));
                        table.Cell().Element(c => SummaryCell(c, "Intereses", Money(data.TotalInterest)));
                        table.Cell().Element(c => SummaryCell(
                            c,
                            "Comisión",
                            data.Commission.HasValue ? Money(data.Commission.Value) : "—"));

                        table.Cell().Element(c => SummaryCell(c, "Total a pagar", Money(data.TotalAmount)));
                        table.Cell().Element(c => SummaryCell(c, "Pagado", Money(data.PaidAmount)));
                        table.Cell().Element(c => SummaryCell(c, "Saldo pendiente", Money(data.BalanceAmount)));
                    });

                    column.Item().PaddingTop(10).Text("Detalle de Cuotas")
                        .FontSize(11)
                        .SemiBold();

                    column.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(22);
                            columns.ConstantColumn(55);
                            columns.RelativeColumn(1.05f);
                            columns.RelativeColumn(0.95f);
                            columns.RelativeColumn(1.05f);
                            columns.RelativeColumn(1.05f);
                            columns.RelativeColumn(0.95f);
                            columns.ConstantColumn(46);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("#");
                            header.Cell().Element(HeaderCellStyle).Text("Vencimiento");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Capital");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Interés");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Cuota");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Saldo");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Pagado");
                            header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Estado");
                        });

                        foreach (var item in data.Items)
                        {
                            var statusText = item.IsCancelled
                                ? "Cancelada"
                                : item.IsPaid
                                    ? "Pagado"
                                    : item.IsOverdue
                                        ? "Vencido"
                                        : "Pendiente";

                            var statusColor = item.IsCancelled
                                ? Colors.Grey.Darken1
                                : item.IsPaid
                                    ? Colors.Green.Darken2
                                    : item.IsOverdue
                                        ? Colors.Red.Darken1
                                        : Colors.Orange.Darken1;

                            table.Cell().Element(c => DataCellStyle(c, item.IsCancelled)).Text(item.PaymentNumber.ToString(CultureInfo.InvariantCulture));
                            table.Cell().Element(c => DataCellStyle(c, item.IsCancelled)).Text($"{item.DueDate:dd/MM/yyyy}");
                            table.Cell().Element(c => DataCellStyle(c, item.IsCancelled)).AlignRight().Text(Money(item.PrincipalAmount));
                            table.Cell().Element(c => DataCellStyle(c, item.IsCancelled)).AlignRight().Text(Money(item.InterestAmount));
                            table.Cell().Element(c => DataCellStyle(c, item.IsCancelled)).AlignRight().Text(Money(item.DueAmount)).SemiBold();
                            table.Cell().Element(c => DataCellStyle(c, item.IsCancelled)).AlignRight().Text(Money(item.RemainingBalance));
                            table.Cell().Element(c => DataCellStyle(c, item.IsCancelled)).AlignRight().Text(Money(item.PaidAmount));
                            table.Cell().Element(c => DataCellStyle(c, item.IsCancelled)).AlignCenter().Text(statusText)
                                .FontColor(statusColor)
                                .FontSize(7.5f);
                        }
                    });

                    column.Item().PaddingTop(8).Background(Colors.Blue.Lighten4)
                        .Padding(8)
                        .Text("Este cronograma de pagos es un documento oficial. Conserve este documento para su referencia.")
                        .FontSize(7.5f)
                        .Italic();
                });
            }

            void SummaryCell(IContainer container, string label, string value)
            {
                container
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Background(Colors.White)
                    .Padding(5)
                    .Column(column =>
                    {
                        column.Item().Text(label)
                            .FontSize(7.5f)
                            .FontColor(Colors.Grey.Darken1);
                        column.Item().Text(value)
                            .FontSize(8.5f)
                            .SemiBold();
                    });
            }

            IContainer HeaderCellStyle(IContainer container)
            {
                return container
                    .Border(1)
                    .BorderColor(Colors.Grey.Darken1)
                    .Background(Colors.Blue.Darken2)
                    .Padding(4)
                    .DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold().FontSize(7.5f));
            }

            IContainer DataCellStyle(IContainer container, bool muted)
            {
                return container
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Background(muted ? Colors.Grey.Lighten4 : Colors.White)
                    .Padding(4)
                    .DefaultTextStyle(x => x.FontSize(7.5f));
            }

            string Money(decimal? amount)
            {
                if (!amount.HasValue)
                {
                    return "—";
                }

                var symbol = data.Currency == "PEN" ? "S/" : "$";
                return $"{symbol} {amount.Value:N2}";
            }

            string InterestTypeLabel(string value) => value switch
            {
                "Simple" => "Simple (interés plano)",
                "French" => "Francés (cuota fija)",
                "Compound" => "Compuesto",
                _ => value,
            };

            string FrequencyLabel(string value) => value switch
            {
                "Daily" => "Diario",
                "Weekly" => "Semanal",
                "Biweekly" => "Quincenal",
                "Monthly" => "Mensual",
                "Quarterly" => "Trimestral",
                _ => value,
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
                _ => value,
            };
        }, cancellationToken);
    }
}
