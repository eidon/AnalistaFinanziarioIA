-- ============================================================
-- AnalistaFinanziarioIA - Dati di Esempio (Seed)
-- ============================================================

USE AnalistaFinanziarioDB;
GO

-- Inserimento titoli di esempio
INSERT INTO Titoli (Simbolo, Nome, Settore, Mercato)
SELECT * FROM (VALUES
    ('ENI',    'Eni S.p.A.',                          'Energia',     'Borsa Italiana'),
    ('ENEL',   'Enel S.p.A.',                         'Utilities',   'Borsa Italiana'),
    ('ISP',    'Intesa Sanpaolo S.p.A.',               'Bancario',    'Borsa Italiana'),
    ('UCG',    'UniCredit S.p.A.',                     'Bancario',    'Borsa Italiana'),
    ('RACE',   'Ferrari N.V.',                         'Automotive',  'Borsa Italiana'),
    ('AAPL',   'Apple Inc.',                           'Tecnologia',  'NASDAQ'),
    ('MSFT',   'Microsoft Corporation',               'Tecnologia',  'NASDAQ'),
    ('GOOGL',  'Alphabet Inc.',                        'Tecnologia',  'NASDAQ')
) AS src(Simbolo, Nome, Settore, Mercato)
WHERE NOT EXISTS (
    SELECT 1 FROM Titoli t WHERE t.Simbolo = src.Simbolo
);
GO

-- Inserimento quotazioni storiche di esempio per ENI
DECLARE @EniId INT = (SELECT Id FROM Titoli WHERE Simbolo = 'ENI');

IF @EniId IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM QuotazioniStoriche WHERE TitoloId = @EniId
)
BEGIN
    INSERT INTO QuotazioniStoriche (TitoloId, Data, PrezzoApertura, PrezzoChiusura, PrezzoMassimo, PrezzoMinimo, Volume)
    VALUES
        (@EniId, '2024-01-02', 14.50, 14.75, 14.85, 14.40, 5200000),
        (@EniId, '2024-01-03', 14.75, 14.60, 14.80, 14.50, 4800000),
        (@EniId, '2024-01-04', 14.60, 14.90, 15.00, 14.55, 6100000),
        (@EniId, '2024-01-05', 14.90, 15.10, 15.20, 14.85, 5700000),
        (@EniId, '2024-01-08', 15.10, 15.05, 15.25, 14.95, 4900000);
END
GO

-- Inserimento analisi di esempio per ENI
DECLARE @EniIdAnalisi INT = (SELECT Id FROM Titoli WHERE Simbolo = 'ENI');

IF @EniIdAnalisi IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM Analisi WHERE TitoloId = @EniIdAnalisi
)
BEGIN
    INSERT INTO Analisi (TitoloId, DataAnalisi, TipoAnalisi, Risultato, Note, PrezzoTarget, Raccomandazione)
    VALUES
        (
            @EniIdAnalisi,
            '2024-01-05',
            'Analisi Tecnica',
            'Il titolo mostra un trend rialzista con supporto a 14.50 e resistenza a 15.50.',
            'RSI in zona neutrale (52). Media mobile a 50 giorni in crescita.',
            15.50,
            'BUY'
        ),
        (
            @EniIdAnalisi,
            '2024-01-05',
            'Analisi Fondamentale',
            'Solidi fondamentali con dividendo stabile e buona generazione di cassa. P/E ratio in linea con il settore.',
            'Dividend yield ~5%. Rapporto Debt/Equity gestibile.',
            16.00,
            'HOLD'
        );
END
GO
