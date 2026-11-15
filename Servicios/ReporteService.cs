using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TuVaca.Comun;
using TuVaca.Models.Reportes;

namespace TuVaca.Servicios
{
    /// <summary>
    /// Arma los archivos de los reportes: Excel con ClosedXML y PDF con QuestPDF.
    /// No consulta la base; recibe las filas ya listas (vienen de los procedimientos almacenados).
    /// </summary>
    public class ReporteService
    {
        private const string ColorTuVaca = "#2E5E3A";

        // ============================== EXCEL ==============================

        public byte[] ProduccionExcel(IReadOnlyList<ReporteProduccionFila> filas, DateTime desde, DateTime hasta, string productor)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Producción");

            hoja.Cell(1, 1).Value = "Tu Vaca - Reporte de producción de leche";
            hoja.Cell(1, 1).Style.Font.Bold = true;
            hoja.Cell(1, 1).Style.Font.FontSize = 14;
            hoja.Cell(2, 1).Value = $"Productor: {productor}";
            hoja.Cell(3, 1).Value = $"Periodo: {Formatos.Fecha(desde)} a {Formatos.Fecha(hasta)}";

            string[] titulos = { "Vaca", "Raza", "Estado", "Días registrados", "Total (L)", "Promedio (L/día)", "Mínimo (L)", "Máximo (L)" };
            const int filaTitulos = 5;
            for (var c = 0; c < titulos.Length; c++)
            {
                hoja.Cell(filaTitulos, c + 1).Value = titulos[c];
            }

            var f = filaTitulos + 1;
            foreach (var fila in filas)
            {
                hoja.Cell(f, 1).Value = fila.Identificacion;
                hoja.Cell(f, 2).Value = fila.Raza;
                hoja.Cell(f, 3).Value = fila.Estado;
                hoja.Cell(f, 4).Value = fila.DiasRegistrados;
                hoja.Cell(f, 5).Value = (double)fila.TotalLitros;
                hoja.Cell(f, 6).Value = (double)fila.PromedioLitros;
                hoja.Cell(f, 7).Value = (double)fila.MinimoLitros;
                hoja.Cell(f, 8).Value = (double)fila.MaximoLitros;
                f++;
            }

            hoja.Cell(f, 4).Value = "TOTAL";
            hoja.Cell(f, 4).Style.Font.Bold = true;
            hoja.Cell(f, 5).Value = (double)filas.Sum(x => x.TotalLitros);
            hoja.Cell(f, 5).Style.Font.Bold = true;

            var encabezado = hoja.Range(filaTitulos, 1, filaTitulos, titulos.Length);
            encabezado.Style.Font.Bold = true;
            encabezado.Style.Font.FontColor = XLColor.White;
            encabezado.Style.Fill.BackgroundColor = XLColor.FromHtml(ColorTuVaca);
            hoja.Range(filaTitulos + 1, 5, f, 8).Style.NumberFormat.Format = "#,##0.00";
            hoja.Columns().AdjustToContents();

            return Guardar(libro);
        }

        public byte[] UsuariosExcel(IReadOnlyList<UsuarioReporteFila> filas)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Usuarios");

            string[] titulos = { "Nombre", "Documento", "Correo", "Teléfono", "Rol", "Estado", "Fecha de registro" };
            for (var c = 0; c < titulos.Length; c++)
            {
                hoja.Cell(1, c + 1).Value = titulos[c];
            }

            var f = 2;
            foreach (var u in filas)
            {
                hoja.Cell(f, 1).Value = u.NombreCompleto;
                hoja.Cell(f, 2).Value = u.NumeroDocumento;
                hoja.Cell(f, 3).Value = u.Correo;
                hoja.Cell(f, 4).Value = u.Telefono ?? string.Empty;
                hoja.Cell(f, 5).Value = u.Rol;
                hoja.Cell(f, 6).Value = u.Estado;
                hoja.Cell(f, 7).Value = u.FechaRegistro;
                hoja.Cell(f, 7).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                f++;
            }

            var encabezado = hoja.Range(1, 1, 1, titulos.Length);
            encabezado.Style.Font.Bold = true;
            encabezado.Style.Font.FontColor = XLColor.White;
            encabezado.Style.Fill.BackgroundColor = XLColor.FromHtml(ColorTuVaca);
            hoja.SheetView.FreezeRows(1);
            hoja.Columns().AdjustToContents();

            return Guardar(libro);
        }

        // =============================== PDF ===============================

        public byte[] ProduccionPdf(IReadOnlyList<ReporteProduccionFila> filas, DateTime desde, DateTime hasta, string productor)
        {
            return Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    Pagina(pagina, "Reporte de producción de leche",
                        $"Productor: {productor} · Periodo: {Formatos.Fecha(desde)} a {Formatos.Fecha(hasta)}");

                    pagina.Content().PaddingTop(10).Column(columna =>
                    {
                        columna.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });

                            tabla.Header(h =>
                            {
                                foreach (var t in new[] { "Vaca", "Raza", "Días", "Total (L)", "Promedio (L/día)", "Mín. (L)", "Máx. (L)" })
                                {
                                    h.Cell().Element(Encabezado).Text(t);
                                }
                            });

                            foreach (var fila in filas)
                            {
                                tabla.Cell().Element(Celda).Text(fila.Identificacion);
                                tabla.Cell().Element(Celda).Text(fila.Raza);
                                tabla.Cell().Element(Celda).AlignRight().Text(fila.DiasRegistrados.ToString());
                                tabla.Cell().Element(Celda).AlignRight().Text(fila.TotalLitros.ToString("0.##"));
                                tabla.Cell().Element(Celda).AlignRight().Text(fila.PromedioLitros.ToString("0.##"));
                                tabla.Cell().Element(Celda).AlignRight().Text(fila.MinimoLitros.ToString("0.##"));
                                tabla.Cell().Element(Celda).AlignRight().Text(fila.MaximoLitros.ToString("0.##"));
                            }
                        });

                        columna.Item().PaddingTop(10).AlignRight()
                            .Text($"Total del hato: {filas.Sum(x => x.TotalLitros):0.##} litros").Bold();

                        if (filas.Count == 0)
                        {
                            columna.Item().PaddingTop(10).Text("No hay vacas hembra registradas.").Italic();
                        }
                    });
                });
            }).GeneratePdf();
        }

        public byte[] SanidadPdf(IReadOnlyList<HistorialSanitarioFila> filas, string vaca, string productor)
        {
            return Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    Pagina(pagina, "Historial sanitario", $"Vaca {vaca} · Productor: {productor}");

                    pagina.Content().PaddingTop(10).Column(columna =>
                    {
                        if (filas.Count == 0)
                        {
                            columna.Item().Text("Esta vaca aún no tiene registros en su historial sanitario.").Italic();
                            return;
                        }

                        columna.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.1f);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1.8f);
                                c.RelativeColumn(1.6f);
                                c.RelativeColumn(1.4f);
                                c.RelativeColumn(2);
                            });

                            tabla.Header(h =>
                            {
                                foreach (var t in new[] { "Fecha", "Tipo", "Nombre", "Responsable", "Próxima dosis", "Observaciones" })
                                {
                                    h.Cell().Element(Encabezado).Text(t);
                                }
                            });

                            foreach (var r in filas)
                            {
                                var proxima = r.ProximaDosis == null
                                    ? "—"
                                    : $"{Formatos.Fecha(r.ProximaDosis.Value)} {r.EstadoProximaDosis}".Trim();
                                tabla.Cell().Element(Celda).Text(Formatos.Fecha(r.FechaAplicacion));
                                tabla.Cell().Element(Celda).Text(r.Tipo);
                                tabla.Cell().Element(Celda).Text(r.Estado == "Anulado" ? $"{r.Nombre} (Anulado)" : r.Nombre);
                                tabla.Cell().Element(Celda).Text(r.Responsable);
                                tabla.Cell().Element(Celda).Text(proxima);
                                tabla.Cell().Element(Celda).Text(r.Observaciones ?? string.Empty);
                            }
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ============================== APOYO ==============================

        private static void Pagina(PageDescriptor pagina, string titulo, string subtitulo)
        {
            pagina.Size(PageSizes.A4.Landscape());
            pagina.Margin(30);
            pagina.DefaultTextStyle(x => x.FontSize(10));

            pagina.Header().Column(c =>
            {
                c.Item().Text("Tu Vaca").FontSize(18).Bold().FontColor(ColorTuVaca);
                c.Item().Text(titulo).FontSize(14).SemiBold();
                c.Item().Text(subtitulo).FontSize(9).FontColor(Colors.Grey.Darken1);
                c.Item().Text($"Generado el {Formatos.FechaHora(DateTime.Now)}").FontSize(8).FontColor(Colors.Grey.Medium);
            });

            pagina.Footer().AlignCenter().Text(t =>
            {
                t.Span("Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        }

        private static IContainer Encabezado(IContainer c) =>
            c.Background(ColorTuVaca).Padding(4).DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold());

        private static IContainer Celda(IContainer c) =>
            c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);

        private static byte[] Guardar(XLWorkbook libro)
        {
            using var memoria = new MemoryStream();
            libro.SaveAs(memoria);
            return memoria.ToArray();
        }
    }
}
