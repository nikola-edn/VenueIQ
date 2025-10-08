# Localization Copy Deck

This document inventories user-facing resource keys and translations (Serbian Latin default, English) and provides context notes.

- Default culture: `sr-Latn`
- Runtime switching: handled by `LocalizationService` and `LocalizationResourceManager`

## Keys (subset, recent stories)

| Key | Serbian (sr-Latn) | English (en) | Context |
|---|---|---|---|
| Main_Analyze | Analiziraj | Analyze | Main analyze button |
| Main_ResetDefaults | Vrati na podrazumevano | Reset to Defaults | Main footer action |
| Main_AdvancedWeights | Napredne težine | Advanced Weights | Weights panel heading |
| Weights_Updating | Ažuriranje… | Updating… | Status pill during recompute |
| Status_Updated | Ažurirano. | Updated. | ARIA announcement after recompute |
| Recompute_PromptRerun | Nedostaju podaci. Prvo pokrenite punu analizu. | Need data. Please run full analysis first. | Inline status near weights |
| Export_Heatmap | Izvezi | Export | Header button |
| Export_Pdf | Izveštaj | Report | Header button |
| Export_Dialog_Title | Izvoz toplotne mape | Export Heatmap | Dialog title |
| Export_Format_Label | Format | Format | Dialog label |
| Export_Resolution_Label | Rezolucija | Resolution | Dialog label |
| Export_Preview_Loading | Priprema pregleda… | Generating preview… | Dialog status |
| Export_Save | Sačuvaj | Save | Dialog button |
| Export_Cancel | Otkaži | Cancel | Dialog button |
| Export_Success | Sačuvano u {0} | Saved to {0} | Dialog status |
| Export_Error | Izvoz nije uspeo. Pokušajte ponovo. | Export failed. Please try again. | Dialog status |
| ExportPdf_Dialog_Title | Izvoz PDF izveštaja | Export PDF Report | Dialog title |
| ExportPdf_Building | Generisanje PDF-a… | Generating PDF… | Progress |
| Tooltip_Title | Detalji | Details | Tooltip heading |
| Tooltip_Complements | Komplementi | Complements | Tooltip section |
| Tooltip_Competitors | Konkurencija | Competitors | Tooltip section |
| Tooltip_NearestTransit | Najbliži prevoz | Nearest transit | Quick metric |
| Tooltip_OpenInMaps | Otvori u mapama | Open in Maps | CTA |
| Tooltip_Close | Zatvori | Close | Close button |
| Tooltip_Opened | Prikazan je detaljni prikaz. | Tooltip opened. | ARIA announcement |
| Map_A11y_MapArea | Interaktivno područje mape | Interactive map area | WebView ARIA description |
| Map_HeatmapUpdated | Toplotna mapa je ažurirana | Heatmap updated | ARIA announcement |
| Map_Error | Greška na mapi: {0} | Map error: {0} | ARIA announcement |
| badge_factor_competition | Konkurencija | Competition | Factor badge label |
| badge_factor_complements | Komplementi | Complements | Factor badge label |
| badge_factor_accessibility | Pristupačnost | Accessibility | Factor badge label |
| badge_factor_demand | Potražnja | Demand | Factor badge label |

Notes:
- For third-party UI (Azure Maps Web control), strings may remain in English; provide ARIA and surrounding copy in Serbian where possible.
- Ensure consistent tone and capitalization per `docs/front-end-spec.md#9-localization-guidelines`.

## Process
- Add new strings to `AppResources.resx` and `AppResources.sr-Latn.resx` with matching keys.
- Reference via `LocalizationResourceManager.Instance["Key"]` (code) or `{Binding [Key], Source={x:Static loc:LocalizationResourceManager.Instance}}` (XAML).
- For accessibility announcements, prefer concise sentences and avoid repetition on rapid updates.

