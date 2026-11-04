using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lending.Infrastructure.Documents;

public sealed class PaymentSchedulePdfGenerator : IPaymentSchedulePdfGenerator
{
    public async Task<byte[]> GeneratePdfAsync(PaymentScheduleData data, CancellationToken cancellationToken = default)
    {
        // Configurar licencia de QuestPDF (Community License para uso no comercial)
        QuestPDF.Settings.License = LicenseType.Community;

        return await Task.Run(() =>
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

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
                        // Logo si existe
                        if (!string.IsNullOrEmpty(data.OrganizationLogoUrl))
                        {
                            try
                            {
                                column.Item().Height(60).Image(data.OrganizationLogoUrl);
                            }
                            catch
                            {
                                // Si falla la carga del logo, mostrar solo el nombre
                                column.Item().Text(data.OrganizationName)
                                    .FontSize(20)
                                    .SemiBold()
                                    .FontColor(Colors.Blue.Darken2);
                            }
                        }
                        else
                        {
                            column.Item().Text(data.OrganizationName)
                                .FontSize(20)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);
                        }

                        column.Item().PaddingTop(5).Text("Cronograma de Pagos")
                            .FontSize(16)
                            .SemiBold();
                    });

                    row.ConstantItem(150).Column(column =>
                    {
                        column.Item().AlignRight().Text($"Préstamo: {data.LoanNumber}")
                            .FontSize(11)
                            .SemiBold();
                        column.Item().AlignRight().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}")
                            .FontSize(9);
                    });
                });
            }

            void ComposeContent(IContainer container)
            {
                container.PaddingVertical(10).Column(column =>
                {
                    // Información del cliente
                    column.Item().Background(Colors.Grey.Lighten3)
                        .Padding(10)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Cliente").FontSize(9).FontColor(Colors.Grey.Darken1);
                                col.Item().Text(data.CustomerName).FontSize(11).SemiBold();
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Documento").FontSize(9).FontColor(Colors.Grey.Darken1);
                                col.Item().Text(data.CustomerDocument).FontSize(11).SemiBold();
                            });
                        });

                    column.Item().PaddingVertical(10);

                    // Resumen del préstamo
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Element(CellStyle).Text("Monto Prestado");
                        table.Cell().Element(CellStyle).Text("Tasa de Interés");
                        table.Cell().Element(CellStyle).Text("Tipo de Interés");
                        table.Cell().Element(CellStyle).Text("Total a Pagar");

                        table.Cell().Element(CellStyle).Text($"S/ {data.PrincipalAmount:N2}");
                        table.Cell().Element(CellStyle).Text($"{data.InterestRate:N2}%");
                        table.Cell().Element(CellStyle).Text(data.InterestType);
                        table.Cell().Element(CellStyle).Text($"S/ {data.TotalAmount:N2}").SemiBold();
                    });

                    column.Item().PaddingVertical(10);

                    // Tabla de cronograma
                    column.Item().Text("Detalle de Cuotas").FontSize(12).SemiBold();
                    column.Item().PaddingVertical(5);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("#");
                            header.Cell().Element(HeaderCellStyle).Text("Fecha Vencimiento");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Monto Cuota");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Pagado");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Pendiente");
                        });

                        // Filas de datos
                        foreach (var item in data.Items)
                        {
                            table.Cell().Element(DataCellStyle).Text($"{item.PaymentNumber}");
                            table.Cell().Element(DataCellStyle).Text($"{item.DueDate:dd/MM/yyyy}");
                            table.Cell().Element(DataCellStyle).AlignRight().Text($"S/ {item.DueAmount:N2}");
                            table.Cell().Element(DataCellStyle).AlignRight().Text($"S/ {item.PaidAmount:N2}");
                            
                            var pendingCell = table.Cell().Element(DataCellStyle).AlignRight();
                            if (item.PendingAmount > 0)
                            {
                                pendingCell.Text($"S/ {item.PendingAmount:N2}").FontColor(Colors.Red.Darken1);
                            }
                            else
                            {
                                pendingCell.Text("S/ 0.00").FontColor(Colors.Green.Darken1);
                            }
                        }
                    });

                    column.Item().PaddingVertical(10);

                    // Nota final
                    column.Item().Background(Colors.Blue.Lighten4)
                        .Padding(10)
                        .Text("Este cronograma de pagos es un documento oficial. Conserve este documento para su referencia.")
                        .FontSize(8)
                        .Italic();
                });
            }

            IContainer CellStyle(IContainer container)
            {
                return container
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten1)
                    .Background(Colors.White)
                    .Padding(5);
            }

            IContainer HeaderCellStyle(IContainer container)
            {
                return container
                    .Border(1)
                    .BorderColor(Colors.Grey.Darken1)
                    .Background(Colors.Blue.Darken2)
                    .Padding(5)
                    .DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold().FontSize(9));
            }

            IContainer DataCellStyle(IContainer container)
            {
                return container
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(5);
            }
        }, cancellationToken);
    }
}
