/* ============================================================
   PROYECTO: TU VACA - Script único para el HOSTING (MonsterASP.NET)

   CÓMO USARLO
   1. Crea la base de datos en el panel de MonsterASP (MSSQL).
   2. Conéctate con SQL Server Management Studio al servidor que te
      da MonsterASP (autenticación SQL Server, usuario y contraseña
      de la base).
   3. En el Explorador de objetos selecciona TU base (la del hosting),
      abre una "Nueva consulta" sobre ella, pega o abre este archivo
      y presiona F5.

   Une los tres scripts (01 corregido + 02 notificaciones +
   03 procedimientos y triggers) SIN las líneas que crean, borran o
   cambian de base de datos, porque en el hosting la base ya existe
   y no se tiene permiso sobre master.
   Ejecútalo UNA sola vez sobre una base vacía.
   ============================================================ */

/* ============================================================
   PROYECTO: TU VACA
   Script de creación de base de datos - SQL Server
   VERSIÓN CORREGIDA según las observaciones del profesor
   Basado en: Especificación de Requerimientos v7 (27-08-2026)
   Autor del documento: Samuel Rincón Rodríguez, Jean Carlo Triviño Pineda

   CORRECCIONES APLICADAS (busca "CORRECCIÓN" dentro del script)
   1. Catálogo de estados centralizado (Estados) para usuarios, vacas,
      publicaciones y registros sanitarios. Nuevo estado "Cancelada"
      para las publicaciones.
   2. Catálogo de causas de muerte (CausasMuerte) + comentario opcional.
   3. El tipo de registro sanitario pasa a ser catálogo
      (TiposRegistroSanitario).
   4. La anulación de los registros sanitarios se maneja solo con el
      estado. La tabla VacunasTratamientos ahora se llama
      RegistrosSanitarios.
   5. Sin datos repetidos del comprador: con su ID se consulta todo lo
      demás (VentasPlataforma y ArriendosLote).
   6. Validación: el comprador debe ser distinto del vendedor.
   7. Catálogos de Departamentos y Municipios (códigos DANE). Las tablas
      guardan solo el MunicipioID: publicaciones, origen de la vaca y
      origen de la venta.
   8. Catálogo de tipo de publicación (TiposPublicacion: Venta / Arriendo).
   ============================================================ */

/* (Hosting) La base ya existe: se crea en el panel de MonsterASP. */


/* ============================================================
   1. CATÁLOGOS
   ============================================================ */

/* ------------------------------------------------------------
   1.1 ROLES (CU-01, RN-4-CU01) - sin cambios
   ------------------------------------------------------------ */
CREATE TABLE Roles (
    RolID           INT IDENTITY(1,1) PRIMARY KEY,
    NombreRol       NVARCHAR(30) NOT NULL UNIQUE
        CHECK (NombreRol IN ('Productor/Vendedor','Comprador/Visitante','Administrador'))
);
GO

INSERT INTO Roles (NombreRol) VALUES
    ('Productor/Vendedor'),
    ('Comprador/Visitante'),
    ('Administrador');
GO

/* ------------------------------------------------------------
   1.2 ESTADOS - CORRECCIÓN 1: catálogo de estados centralizado
   Una sola tabla con todos los estados del sistema. La columna
   Ambito dice a qué tabla pertenece cada estado.
   Cada tabla que tiene estado lleva una columna calculada fija
   (AmbitoEstado) que, junto con EstadoID, forma una llave foránea
   compuesta. Así SQL Server solo deja asignar a cada tabla los
   estados de su propio ámbito (por ejemplo, a una vaca no se le
   puede poner 'Pausada'). AmbitoEstado se llena sola: nunca se
   inserta ni se actualiza.
   Los ID son fijos porque el código y algunas reglas los usan.
   ------------------------------------------------------------ */
CREATE TABLE Estados (
    EstadoID    INT          NOT NULL PRIMARY KEY,
    Nombre      NVARCHAR(30) NOT NULL,
    Ambito      VARCHAR(20)  NOT NULL
        CHECK (Ambito IN ('USUARIO','VACA','PUBLICACION','REGISTRO_SANITARIO')),
    CONSTRAINT UQ_Estados_NombreAmbito UNIQUE (Nombre, Ambito),
    CONSTRAINT UQ_Estados_EstadoAmbito UNIQUE (EstadoID, Ambito)
);
GO

INSERT INTO Estados (EstadoID, Nombre, Ambito) VALUES
    -- Usuarios (CU-10)
    (1,  N'Activo',                'USUARIO'),
    (2,  N'Inactivo',              'USUARIO'),
    -- Vacas (CU-13, CU-14, CU-15)
    (10, N'Viva',                  'VACA'),
    (11, N'Fallecida',             'VACA'),
    (12, N'Vendida en plataforma', 'VACA'),
    (13, N'Vendida externamente',  'VACA'),
    -- Publicaciones de vacas y de lotes (CU-03, CU-04, CU-09)
    (20, N'Activa',                'PUBLICACION'),
    (21, N'Pausada',               'PUBLICACION'),
    (22, N'Vendida',               'PUBLICACION'),
    (23, N'Arrendada',             'PUBLICACION'),
    (24, N'Rechazada',             'PUBLICACION'),  -- la rechaza el administrador
    (25, N'Cancelada',             'PUBLICACION'),  -- NUEVO: la cancela el vendedor
    (26, N'Eliminada',             'PUBLICACION'),  -- la elimina el administrador
    -- Registros sanitarios (RN-5-CU07, RN-3-CU21)
    (30, N'Activo',                'REGISTRO_SANITARIO'),
    (31, N'Anulado',               'REGISTRO_SANITARIO');
GO

/* ------------------------------------------------------------
   1.3 TIPOS DE PUBLICACIÓN - CORRECCIÓN 8
   Reemplaza la columna de texto TipoNegociacion de los lotes.
   ------------------------------------------------------------ */
CREATE TABLE TiposPublicacion (
    TipoPublicacionID   INT IDENTITY(1,1) PRIMARY KEY,
    Nombre              NVARCHAR(30) NOT NULL UNIQUE
);
GO

INSERT INTO TiposPublicacion (Nombre) VALUES
    (N'Venta'),
    (N'Arriendo');
GO

/* ------------------------------------------------------------
   1.4 TIPOS DE REGISTRO SANITARIO - CORRECCIÓN 3
   El tipo sí es necesario (una vacuna y un tratamiento se
   registran y se consultan por separado), pero ahora es un
   catálogo y no un texto.
   ------------------------------------------------------------ */
CREATE TABLE TiposRegistroSanitario (
    TipoRegistroID  INT IDENTITY(1,1) PRIMARY KEY,
    Nombre          NVARCHAR(30) NOT NULL UNIQUE
);
GO

INSERT INTO TiposRegistroSanitario (Nombre) VALUES
    (N'Vacuna'),
    (N'Tratamiento');
GO

/* ------------------------------------------------------------
   1.5 CAUSAS DE MUERTE - CORRECCIÓN 2 (CU-13)
   ------------------------------------------------------------ */
CREATE TABLE CausasMuerte (
    CausaMuerteID   INT IDENTITY(1,1) PRIMARY KEY,
    Nombre          NVARCHAR(60) NOT NULL UNIQUE
);
GO

INSERT INTO CausasMuerte (Nombre) VALUES
    (N'Enfermedad'),
    (N'Accidente'),
    (N'Complicaciones en el parto'),
    (N'Ataque de otro animal'),
    (N'Intoxicación'),
    (N'Vejez'),
    (N'Desconocida'),
    (N'Otra');
GO

/* ------------------------------------------------------------
   1.6 DEPARTAMENTOS Y MUNICIPIOS - CORRECCIÓN 7
   Códigos DANE (DIVIPOLA). Nombres como en el listado oficial de
   la DIAN (en mayúsculas). Las demás tablas guardan SOLO el
   MunicipioID: el departamento se obtiene con un JOIN a Municipios.
   ------------------------------------------------------------ */
CREATE TABLE Departamentos (
    DepartamentoID  INT IDENTITY(1,1) PRIMARY KEY,
    CodigoDane      CHAR(2)      NOT NULL UNIQUE,
    Nombre          NVARCHAR(80) NOT NULL UNIQUE
);
GO

CREATE TABLE Municipios (
    MunicipioID     INT IDENTITY(1,1) PRIMARY KEY,
    CodigoDane      CHAR(5)      NOT NULL UNIQUE,
    Nombre          NVARCHAR(80) NOT NULL,
    DepartamentoID  INT          NOT NULL,
    CONSTRAINT FK_Municipios_Departamentos FOREIGN KEY (DepartamentoID)
        REFERENCES Departamentos(DepartamentoID),
    CONSTRAINT UQ_Municipios_NombreDepartamento UNIQUE (Nombre, DepartamentoID)
);
GO

CREATE INDEX IX_Municipios_DepartamentoID ON Municipios(DepartamentoID);
GO

INSERT INTO Departamentos (CodigoDane, Nombre) VALUES
    ('05', N'ANTIOQUIA'),
    ('08', N'ATLANTICO'),
    ('11', N'BOGOTA D.C.'),
    ('13', N'BOLIVAR'),
    ('15', N'BOYACA'),
    ('17', N'CALDAS'),
    ('18', N'CAQUETA'),
    ('19', N'CAUCA'),
    ('20', N'CESAR'),
    ('23', N'CORDOBA'),
    ('25', N'CUNDINAMARCA'),
    ('27', N'CHOCO'),
    ('41', N'HUILA'),
    ('44', N'LA GUAJIRA'),
    ('47', N'MAGDALENA'),
    ('50', N'META'),
    ('52', N'NARIÑO'),
    ('54', N'NORTE DE SANTANDER'),
    ('63', N'QUINDIO'),
    ('66', N'RISARALDA'),
    ('68', N'SANTANDER'),
    ('70', N'SUCRE'),
    ('73', N'TOLIMA'),
    ('76', N'VALLE DEL CAUCA'),
    ('81', N'ARAUCA'),
    ('85', N'CASANARE'),
    ('86', N'PUTUMAYO'),
    ('88', N'SAN ANDRES, PROVIDENCIA Y SANTA CATALINA'),
    ('91', N'AMAZONAS'),
    ('94', N'GUAINIA'),
    ('95', N'GUAVIARE'),
    ('97', N'VAUPES'),
    ('99', N'VICHADA');
GO

/* Todos los municipios de Colombia (incluye Bogotá D.C. y las áreas
   no municipalizadas). El departamento se toma de los 2 primeros
   dígitos del código DANE. */
INSERT INTO Municipios (CodigoDane, Nombre, DepartamentoID)
SELECT m.CodigoDane, m.Nombre, d.DepartamentoID
FROM (VALUES
    -- ANTIOQUIA
    ('05001', N'MEDELLIN'),
    ('05002', N'ABEJORRAL'),
    ('05004', N'ABRIAQUI'),
    ('05021', N'ALEJANDRIA'),
    ('05030', N'AMAGA'),
    ('05031', N'AMALFI'),
    ('05034', N'ANDES'),
    ('05036', N'ANGELOPOLIS'),
    ('05038', N'ANGOSTURA'),
    ('05040', N'ANORI'),
    ('05042', N'SANTAFE DE ANTIOQUIA'),
    ('05044', N'ANZA'),
    ('05045', N'APARTADO'),
    ('05051', N'ARBOLETES'),
    ('05055', N'ARGELIA'),
    ('05059', N'ARMENIA'),
    ('05079', N'BARBOSA'),
    ('05086', N'BELMIRA'),
    ('05088', N'BELLO'),
    ('05091', N'BETANIA'),
    ('05093', N'BETULIA'),
    ('05101', N'CIUDAD BOLIVAR'),
    ('05107', N'BRICEÑO'),
    ('05113', N'BURITICA'),
    ('05120', N'CACERES'),
    ('05125', N'CAICEDO'),
    ('05129', N'CALDAS'),
    ('05134', N'CAMPAMENTO'),
    ('05138', N'CAÑASGORDAS'),
    ('05142', N'CARACOLI'),
    ('05145', N'CARAMANTA'),
    ('05147', N'CAREPA'),
    ('05148', N'EL CARMEN DE VIBORAL'),
    ('05150', N'CAROLINA'),
    ('05154', N'CAUCASIA'),
    ('05172', N'CHIGORODO'),
    ('05190', N'CISNEROS'),
    ('05197', N'COCORNA'),
    ('05206', N'CONCEPCION'),
    ('05209', N'CONCORDIA'),
    ('05212', N'COPACABANA'),
    ('05234', N'DABEIBA'),
    ('05237', N'DON MATIAS'),
    ('05240', N'EBEJICO'),
    ('05250', N'EL BAGRE'),
    ('05264', N'ENTRERRIOS'),
    ('05266', N'ENVIGADO'),
    ('05282', N'FREDONIA'),
    ('05284', N'FRONTINO'),
    ('05306', N'GIRALDO'),
    ('05308', N'GIRARDOTA'),
    ('05310', N'GOMEZ PLATA'),
    ('05313', N'GRANADA'),
    ('05315', N'GUADALUPE'),
    ('05318', N'GUARNE'),
    ('05321', N'GUATAPE'),
    ('05347', N'HELICONIA'),
    ('05353', N'HISPANIA'),
    ('05360', N'ITAGUI'),
    ('05361', N'ITUANGO'),
    ('05364', N'JARDIN'),
    ('05368', N'JERICO'),
    ('05376', N'LA CEJA'),
    ('05380', N'LA ESTRELLA'),
    ('05390', N'LA PINTADA'),
    ('05400', N'LA UNION'),
    ('05411', N'LIBORINA'),
    ('05425', N'MACEO'),
    ('05440', N'MARINILLA'),
    ('05467', N'MONTEBELLO'),
    ('05475', N'MURINDO'),
    ('05480', N'MUTATA'),
    ('05483', N'NARIÑO'),
    ('05490', N'NECOCLI'),
    ('05495', N'NECHI'),
    ('05501', N'OLAYA'),
    ('05541', N'PEÑOL'),
    ('05543', N'PEQUE'),
    ('05576', N'PUEBLORRICO'),
    ('05579', N'PUERTO BERRIO'),
    ('05585', N'PUERTO NARE'),
    ('05591', N'PUERTO TRIUNFO'),
    ('05604', N'REMEDIOS'),
    ('05607', N'RETIRO'),
    ('05615', N'RIONEGRO'),
    ('05628', N'SABANALARGA'),
    ('05631', N'SABANETA'),
    ('05642', N'SALGAR'),
    ('05647', N'SAN ANDRES DE CUERQUIA'),
    ('05649', N'SAN CARLOS'),
    ('05652', N'SAN FRANCISCO'),
    ('05656', N'SAN JERONIMO'),
    ('05658', N'SAN JOSE DE LA MONTAÑA'),
    ('05659', N'SAN JUAN DE URABA'),
    ('05660', N'SAN LUIS'),
    ('05664', N'SAN PEDRO'),
    ('05665', N'SAN PEDRO DE URABA'),
    ('05667', N'SAN RAFAEL'),
    ('05670', N'SAN ROQUE'),
    ('05674', N'SAN VICENTE'),
    ('05679', N'SANTA BARBARA'),
    ('05686', N'SANTA ROSA DE OSOS'),
    ('05690', N'SANTO DOMINGO'),
    ('05697', N'EL SANTUARIO'),
    ('05736', N'SEGOVIA'),
    ('05756', N'SONSON'),
    ('05761', N'SOPETRAN'),
    ('05789', N'TAMESIS'),
    ('05790', N'TARAZA'),
    ('05792', N'TARSO'),
    ('05809', N'TITIRIBI'),
    ('05819', N'TOLEDO'),
    ('05837', N'TURBO'),
    ('05842', N'URAMITA'),
    ('05847', N'URRAO'),
    ('05854', N'VALDIVIA'),
    ('05856', N'VALPARAISO'),
    ('05858', N'VEGACHI'),
    ('05861', N'VENECIA'),
    ('05873', N'VIGIA DEL FUERTE'),
    ('05885', N'YALI'),
    ('05887', N'YARUMAL'),
    ('05890', N'YOLOMBO'),
    ('05893', N'YONDO'),
    ('05895', N'ZARAGOZA'),
    -- ATLANTICO
    ('08001', N'BARRANQUILLA'),
    ('08078', N'BARANOA'),
    ('08137', N'CAMPO DE LA CRUZ'),
    ('08141', N'CANDELARIA'),
    ('08296', N'GALAPA'),
    ('08372', N'JUAN DE ACOSTA'),
    ('08421', N'LURUACO'),
    ('08433', N'MALAMBO'),
    ('08436', N'MANATI'),
    ('08520', N'PALMAR DE VARELA'),
    ('08549', N'PIOJO'),
    ('08558', N'POLONUEVO'),
    ('08560', N'PONEDERA'),
    ('08573', N'PUERTO COLOMBIA'),
    ('08606', N'REPELON'),
    ('08634', N'SABANAGRANDE'),
    ('08638', N'SABANALARGA'),
    ('08675', N'SANTA LUCIA'),
    ('08685', N'SANTO TOMAS'),
    ('08758', N'SOLEDAD'),
    ('08770', N'SUAN'),
    ('08832', N'TUBARA'),
    ('08849', N'USIACURI'),
    -- BOGOTA D.C.
    ('11001', N'BOGOTA D.C.'),
    -- BOLIVAR
    ('13001', N'CARTAGENA'),
    ('13006', N'ACHI'),
    ('13030', N'ALTOS DEL ROSARIO'),
    ('13042', N'ARENAL'),
    ('13052', N'ARJONA'),
    ('13062', N'ARROYOHONDO'),
    ('13074', N'BARRANCO DE LOBA'),
    ('13140', N'CALAMAR'),
    ('13160', N'CANTAGALLO'),
    ('13188', N'CICUCO'),
    ('13212', N'CORDOBA'),
    ('13222', N'CLEMENCIA'),
    ('13244', N'EL CARMEN DE BOLIVAR'),
    ('13248', N'EL GUAMO'),
    ('13268', N'EL PEÑON'),
    ('13300', N'HATILLO DE LOBA'),
    ('13430', N'MAGANGUE'),
    ('13433', N'MAHATES'),
    ('13440', N'MARGARITA'),
    ('13442', N'MARIA LA BAJA'),
    ('13458', N'MONTECRISTO'),
    ('13468', N'MOMPOS'),
    ('13473', N'MORALES'),
    ('13490', N'NOROSI'),
    ('13549', N'PINILLOS'),
    ('13580', N'REGIDOR'),
    ('13600', N'RIO VIEJO'),
    ('13620', N'SAN CRISTOBAL'),
    ('13647', N'SAN ESTANISLAO'),
    ('13650', N'SAN FERNANDO'),
    ('13654', N'SAN JACINTO'),
    ('13655', N'SAN JACINTO DEL CAUCA'),
    ('13657', N'SAN JUAN NEPOMUCENO'),
    ('13667', N'SAN MARTIN DE LOBA'),
    ('13670', N'SAN PABLO'),
    ('13673', N'SANTA CATALINA'),
    ('13683', N'SANTA ROSA'),
    ('13688', N'SANTA ROSA DEL SUR'),
    ('13744', N'SIMITI'),
    ('13760', N'SOPLAVIENTO'),
    ('13780', N'TALAIGUA NUEVO'),
    ('13810', N'TIQUISIO'),
    ('13836', N'TURBACO'),
    ('13838', N'TURBANA'),
    ('13873', N'VILLANUEVA'),
    ('13894', N'ZAMBRANO'),
    -- BOYACA
    ('15001', N'TUNJA'),
    ('15022', N'ALMEIDA'),
    ('15047', N'AQUITANIA'),
    ('15051', N'ARCABUCO'),
    ('15087', N'BELEN'),
    ('15090', N'BERBEO'),
    ('15092', N'BETEITIVA'),
    ('15097', N'BOAVITA'),
    ('15104', N'BOYACA'),
    ('15106', N'BRICEÑO'),
    ('15109', N'BUENAVISTA'),
    ('15114', N'BUSBANZA'),
    ('15131', N'CALDAS'),
    ('15135', N'CAMPOHERMOSO'),
    ('15162', N'CERINZA'),
    ('15172', N'CHINAVITA'),
    ('15176', N'CHIQUINQUIRA'),
    ('15180', N'CHISCAS'),
    ('15183', N'CHITA'),
    ('15185', N'CHITARAQUE'),
    ('15187', N'CHIVATA'),
    ('15189', N'CIENEGA'),
    ('15204', N'COMBITA'),
    ('15212', N'COPER'),
    ('15215', N'CORRALES'),
    ('15218', N'COVARACHIA'),
    ('15223', N'CUBARA'),
    ('15224', N'CUCAITA'),
    ('15226', N'CUITIVA'),
    ('15232', N'CHIQUIZA'),
    ('15236', N'CHIVOR'),
    ('15238', N'DUITAMA'),
    ('15244', N'EL COCUY'),
    ('15248', N'EL ESPINO'),
    ('15272', N'FIRAVITOBA'),
    ('15276', N'FLORESTA'),
    ('15293', N'GACHANTIVA'),
    ('15296', N'GAMEZA'),
    ('15299', N'GARAGOA'),
    ('15317', N'GUACAMAYAS'),
    ('15322', N'GUATEQUE'),
    ('15325', N'GUAYATA'),
    ('15332', N'GUICAN'),
    ('15362', N'IZA'),
    ('15367', N'JENESANO'),
    ('15368', N'JERICO'),
    ('15377', N'LABRANZAGRANDE'),
    ('15380', N'LA CAPILLA'),
    ('15401', N'LA VICTORIA'),
    ('15403', N'LA UVITA'),
    ('15407', N'VILLA DE LEYVA'),
    ('15425', N'MACANAL'),
    ('15442', N'MARIPI'),
    ('15455', N'MIRAFLORES'),
    ('15464', N'MONGUA'),
    ('15466', N'MONGUI'),
    ('15469', N'MONIQUIRA'),
    ('15476', N'MOTAVITA'),
    ('15480', N'MUZO'),
    ('15491', N'NOBSA'),
    ('15494', N'NUEVO COLON'),
    ('15500', N'OICATA'),
    ('15507', N'OTANCHE'),
    ('15511', N'PACHAVITA'),
    ('15514', N'PAEZ'),
    ('15516', N'PAIPA'),
    ('15518', N'PAJARITO'),
    ('15522', N'PANQUEBA'),
    ('15531', N'PAUNA'),
    ('15533', N'PAYA'),
    ('15537', N'PAZ DE RIO'),
    ('15542', N'PESCA'),
    ('15550', N'PISBA'),
    ('15572', N'PUERTO BOYACA'),
    ('15580', N'QUIPAMA'),
    ('15599', N'RAMIRIQUI'),
    ('15600', N'RAQUIRA'),
    ('15621', N'RONDON'),
    ('15632', N'SABOYA'),
    ('15638', N'SACHICA'),
    ('15646', N'SAMACA'),
    ('15660', N'SAN EDUARDO'),
    ('15664', N'SAN JOSE DE PARE'),
    ('15667', N'SAN LUIS DE GACENO'),
    ('15673', N'SAN MATEO'),
    ('15676', N'SAN MIGUEL DE SEMA'),
    ('15681', N'SAN PABLO DE BORBUR'),
    ('15686', N'SANTANA'),
    ('15690', N'SANTA MARIA'),
    ('15693', N'SANTA ROSA DE VITERBO'),
    ('15696', N'SANTA SOFIA'),
    ('15720', N'SATIVANORTE'),
    ('15723', N'SATIVASUR'),
    ('15740', N'SIACHOQUE'),
    ('15753', N'SOATA'),
    ('15755', N'SOCOTA'),
    ('15757', N'SOCHA'),
    ('15759', N'SOGAMOSO'),
    ('15761', N'SOMONDOCO'),
    ('15762', N'SORA'),
    ('15763', N'SOTAQUIRA'),
    ('15764', N'SORACA'),
    ('15774', N'SUSACON'),
    ('15776', N'SUTAMARCHAN'),
    ('15778', N'SUTATENZA'),
    ('15790', N'TASCO'),
    ('15798', N'TENZA'),
    ('15804', N'TIBANA'),
    ('15806', N'TIBASOSA'),
    ('15808', N'TINJACA'),
    ('15810', N'TIPACOQUE'),
    ('15814', N'TOCA'),
    ('15816', N'TOGUI'),
    ('15820', N'TOPAGA'),
    ('15822', N'TOTA'),
    ('15832', N'TUNUNGUA'),
    ('15835', N'TURMEQUE'),
    ('15837', N'TUTA'),
    ('15839', N'TUTAZA'),
    ('15842', N'UMBITA'),
    ('15861', N'VENTAQUEMADA'),
    ('15879', N'VIRACACHA'),
    ('15897', N'ZETAQUIRA'),
    -- CALDAS
    ('17001', N'MANIZALES'),
    ('17013', N'AGUADAS'),
    ('17042', N'ANSERMA'),
    ('17050', N'ARANZAZU'),
    ('17088', N'BELALCAZAR'),
    ('17174', N'CHINCHINA'),
    ('17272', N'FILADELFIA'),
    ('17380', N'LA DORADA'),
    ('17388', N'LA MERCED'),
    ('17433', N'MANZANARES'),
    ('17442', N'MARMATO'),
    ('17444', N'MARQUETALIA'),
    ('17446', N'MARULANDA'),
    ('17486', N'NEIRA'),
    ('17495', N'NORCASIA'),
    ('17513', N'PACORA'),
    ('17524', N'PALESTINA'),
    ('17541', N'PENSILVANIA'),
    ('17614', N'RIOSUCIO'),
    ('17616', N'RISARALDA'),
    ('17653', N'SALAMINA'),
    ('17662', N'SAMANA'),
    ('17665', N'SAN JOSE'),
    ('17777', N'SUPIA'),
    ('17867', N'VICTORIA'),
    ('17873', N'VILLAMARIA'),
    ('17877', N'VITERBO'),
    -- CAQUETA
    ('18001', N'FLORENCIA'),
    ('18029', N'ALBANIA'),
    ('18094', N'BELEN DE LOS ANDAQUIES'),
    ('18150', N'CARTAGENA DEL CHAIRA'),
    ('18205', N'CURILLO'),
    ('18247', N'EL DONCELLO'),
    ('18256', N'EL PAUJIL'),
    ('18410', N'LA MONTAÑITA'),
    ('18460', N'MILAN'),
    ('18479', N'MORELIA'),
    ('18592', N'PUERTO RICO'),
    ('18610', N'SAN JOSE DEL FRAGUA'),
    ('18753', N'SAN VICENTE DEL CAGUAN'),
    ('18756', N'SOLANO'),
    ('18785', N'SOLITA'),
    ('18860', N'VALPARAISO'),
    -- CAUCA
    ('19001', N'POPAYAN'),
    ('19022', N'ALMAGUER'),
    ('19050', N'ARGELIA'),
    ('19075', N'BALBOA'),
    ('19100', N'BOLIVAR'),
    ('19110', N'BUENOS AIRES'),
    ('19130', N'CAJIBIO'),
    ('19137', N'CALDONO'),
    ('19142', N'CALOTO'),
    ('19212', N'CORINTO'),
    ('19256', N'EL TAMBO'),
    ('19290', N'FLORENCIA'),
    ('19300', N'GUACHENE'),
    ('19318', N'GUAPI'),
    ('19355', N'INZA'),
    ('19364', N'JAMBALO'),
    ('19392', N'LA SIERRA'),
    ('19397', N'LA VEGA'),
    ('19418', N'LOPEZ'),
    ('19450', N'MERCADERES'),
    ('19455', N'MIRANDA'),
    ('19473', N'MORALES'),
    ('19513', N'PADILLA'),
    ('19517', N'PAEZ'),
    ('19532', N'PATIA'),
    ('19533', N'PIAMONTE'),
    ('19548', N'PIENDAMO'),
    ('19573', N'PUERTO TEJADA'),
    ('19585', N'PURACE'),
    ('19622', N'ROSAS'),
    ('19693', N'SAN SEBASTIAN'),
    ('19698', N'SANTANDER DE QUILICHAO'),
    ('19701', N'SANTA ROSA'),
    ('19743', N'SILVIA'),
    ('19760', N'SOTARA'),
    ('19780', N'SUAREZ'),
    ('19785', N'SUCRE'),
    ('19807', N'TIMBIO'),
    ('19809', N'TIMBIQUI'),
    ('19821', N'TORIBIO'),
    ('19824', N'TOTORO'),
    ('19845', N'VILLA RICA'),
    -- CESAR
    ('20001', N'VALLEDUPAR'),
    ('20011', N'AGUACHICA'),
    ('20013', N'AGUSTIN CODAZZI'),
    ('20032', N'ASTREA'),
    ('20045', N'BECERRIL'),
    ('20060', N'BOSCONIA'),
    ('20175', N'CHIMICHAGUA'),
    ('20178', N'CHIRIGUANA'),
    ('20228', N'CURUMANI'),
    ('20238', N'EL COPEY'),
    ('20250', N'EL PASO'),
    ('20295', N'GAMARRA'),
    ('20310', N'GONZALEZ'),
    ('20383', N'LA GLORIA'),
    ('20400', N'LA JAGUA DE IBIRICO'),
    ('20443', N'MANAURE'),
    ('20517', N'PAILITAS'),
    ('20550', N'PELAYA'),
    ('20570', N'PUEBLO BELLO'),
    ('20614', N'RIO DE ORO'),
    ('20621', N'LA PAZ'),
    ('20710', N'SAN ALBERTO'),
    ('20750', N'SAN DIEGO'),
    ('20770', N'SAN MARTIN'),
    ('20787', N'TAMALAMEQUE'),
    -- CORDOBA
    ('23001', N'MONTERIA'),
    ('23068', N'AYAPEL'),
    ('23079', N'BUENAVISTA'),
    ('23090', N'CANALETE'),
    ('23162', N'CERETE'),
    ('23168', N'CHIMA'),
    ('23182', N'CHINU'),
    ('23189', N'CIENAGA DE ORO'),
    ('23300', N'COTORRA'),
    ('23350', N'LA APARTADA'),
    ('23417', N'LORICA'),
    ('23419', N'LOS CORDOBAS'),
    ('23464', N'MOMIL'),
    ('23466', N'MONTELIBANO'),
    ('23500', N'MOÑITOS'),
    ('23555', N'PLANETA RICA'),
    ('23570', N'PUEBLO NUEVO'),
    ('23574', N'PUERTO ESCONDIDO'),
    ('23580', N'PUERTO LIBERTADOR'),
    ('23586', N'PURISIMA'),
    ('23660', N'SAHAGUN'),
    ('23670', N'SAN ANDRES SOTAVENTO'),
    ('23672', N'SAN ANTERO'),
    ('23675', N'SAN BERNARDO DEL VIENTO'),
    ('23678', N'SAN CARLOS'),
    ('23682', N'SAN JOSE DE URE'),
    ('23686', N'SAN PELAYO'),
    ('23807', N'TIERRALTA'),
    ('23815', N'TUCHIN'),
    ('23855', N'VALENCIA'),
    -- CUNDINAMARCA
    ('25001', N'AGUA DE DIOS'),
    ('25019', N'ALBAN'),
    ('25035', N'ANAPOIMA'),
    ('25040', N'ANOLAIMA'),
    ('25053', N'ARBELAEZ'),
    ('25086', N'BELTRAN'),
    ('25095', N'BITUIMA'),
    ('25099', N'BOJACA'),
    ('25120', N'CABRERA'),
    ('25123', N'CACHIPAY'),
    ('25126', N'CAJICA'),
    ('25148', N'CAPARRAPI'),
    ('25151', N'CAQUEZA'),
    ('25154', N'CARMEN DE CARUPA'),
    ('25168', N'CHAGUANI'),
    ('25175', N'CHIA'),
    ('25178', N'CHIPAQUE'),
    ('25181', N'CHOACHI'),
    ('25183', N'CHOCONTA'),
    ('25200', N'COGUA'),
    ('25214', N'COTA'),
    ('25224', N'CUCUNUBA'),
    ('25245', N'EL COLEGIO'),
    ('25258', N'EL PEÑON'),
    ('25260', N'EL ROSAL'),
    ('25269', N'FACATATIVA'),
    ('25279', N'FOMEQUE'),
    ('25281', N'FOSCA'),
    ('25286', N'FUNZA'),
    ('25288', N'FUQUENE'),
    ('25290', N'FUSAGASUGA'),
    ('25293', N'GACHALA'),
    ('25295', N'GACHANCIPA'),
    ('25297', N'GACHETA'),
    ('25299', N'GAMA'),
    ('25307', N'GIRARDOT'),
    ('25312', N'GRANADA'),
    ('25317', N'GUACHETA'),
    ('25320', N'GUADUAS'),
    ('25322', N'GUASCA'),
    ('25324', N'GUATAQUI'),
    ('25326', N'GUATAVITA'),
    ('25328', N'GUAYABAL DE SIQUIMA'),
    ('25335', N'GUAYABETAL'),
    ('25339', N'GUTIERREZ'),
    ('25368', N'JERUSALEN'),
    ('25372', N'JUNIN'),
    ('25377', N'LA CALERA'),
    ('25386', N'LA MESA'),
    ('25394', N'LA PALMA'),
    ('25398', N'LA PEÑA'),
    ('25402', N'LA VEGA'),
    ('25407', N'LENGUAZAQUE'),
    ('25426', N'MACHETA'),
    ('25430', N'MADRID'),
    ('25436', N'MANTA'),
    ('25438', N'MEDINA'),
    ('25473', N'MOSQUERA'),
    ('25483', N'NARIÑO'),
    ('25486', N'NEMOCON'),
    ('25488', N'NILO'),
    ('25489', N'NIMAIMA'),
    ('25491', N'NOCAIMA'),
    ('25506', N'VENECIA'),
    ('25513', N'PACHO'),
    ('25518', N'PAIME'),
    ('25524', N'PANDI'),
    ('25530', N'PARATEBUENO'),
    ('25535', N'PASCA'),
    ('25572', N'PUERTO SALGAR'),
    ('25580', N'PULI'),
    ('25592', N'QUEBRADANEGRA'),
    ('25594', N'QUETAME'),
    ('25596', N'QUIPILE'),
    ('25599', N'APULO'),
    ('25612', N'RICAURTE'),
    ('25645', N'SAN ANTONIO DEL TEQUENDAMA'),
    ('25649', N'SAN BERNARDO'),
    ('25653', N'SAN CAYETANO'),
    ('25658', N'SAN FRANCISCO'),
    ('25662', N'SAN JUAN DE RIO SECO'),
    ('25718', N'SASAIMA'),
    ('25736', N'SESQUILE'),
    ('25740', N'SIBATE'),
    ('25743', N'SILVANIA'),
    ('25745', N'SIMIJACA'),
    ('25754', N'SOACHA'),
    ('25758', N'SOPO'),
    ('25769', N'SUBACHOQUE'),
    ('25772', N'SUESCA'),
    ('25777', N'SUPATA'),
    ('25779', N'SUSA'),
    ('25781', N'SUTATAUSA'),
    ('25785', N'TABIO'),
    ('25793', N'TAUSA'),
    ('25797', N'TENA'),
    ('25799', N'TENJO'),
    ('25805', N'TIBACUY'),
    ('25807', N'TIBIRITA'),
    ('25815', N'TOCAIMA'),
    ('25817', N'TOCANCIPA'),
    ('25823', N'TOPAIPI'),
    ('25839', N'UBALA'),
    ('25841', N'UBAQUE'),
    ('25843', N'VILLA DE SAN DIEGO DE UBATE'),
    ('25845', N'UNE'),
    ('25851', N'UTICA'),
    ('25862', N'VERGARA'),
    ('25867', N'VIANI'),
    ('25871', N'VILLAGOMEZ'),
    ('25873', N'VILLAPINZON'),
    ('25875', N'VILLETA'),
    ('25878', N'VIOTA'),
    ('25885', N'YACOPI'),
    ('25898', N'ZIPACON'),
    ('25899', N'ZIPAQUIRA'),
    -- CHOCO
    ('27001', N'QUIBDO'),
    ('27006', N'ACANDI'),
    ('27025', N'ALTO BAUDO'),
    ('27050', N'ATRATO'),
    ('27073', N'BAGADO'),
    ('27075', N'BAHIA SOLANO'),
    ('27077', N'BAJO BAUDO'),
    ('27099', N'BOJAYA'),
    ('27135', N'EL CANTON DEL SAN PABLO'),
    ('27150', N'CARMEN DEL DARIEN'),
    ('27160', N'CERTEGUI'),
    ('27205', N'CONDOTO'),
    ('27245', N'EL CARMEN DE ATRATO'),
    ('27250', N'EL LITORAL DEL SAN JUAN'),
    ('27361', N'ISTMINA'),
    ('27372', N'JURADO'),
    ('27413', N'LLORO'),
    ('27425', N'MEDIO ATRATO'),
    ('27430', N'MEDIO BAUDO'),
    ('27450', N'MEDIO SAN JUAN'),
    ('27491', N'NOVITA'),
    ('27495', N'NUQUI'),
    ('27580', N'RIO IRO'),
    ('27600', N'RIO QUITO'),
    ('27615', N'RIOSUCIO'),
    ('27660', N'SAN JOSE DEL PALMAR'),
    ('27745', N'SIPI'),
    ('27787', N'TADO'),
    ('27800', N'UNGUIA'),
    ('27810', N'UNION PANAMERICANA'),
    -- HUILA
    ('41001', N'NEIVA'),
    ('41006', N'ACEVEDO'),
    ('41013', N'AGRADO'),
    ('41016', N'AIPE'),
    ('41020', N'ALGECIRAS'),
    ('41026', N'ALTAMIRA'),
    ('41078', N'BARAYA'),
    ('41132', N'CAMPOALEGRE'),
    ('41206', N'COLOMBIA'),
    ('41244', N'ELIAS'),
    ('41298', N'GARZON'),
    ('41306', N'GIGANTE'),
    ('41319', N'GUADALUPE'),
    ('41349', N'HOBO'),
    ('41357', N'IQUIRA'),
    ('41359', N'ISNOS'),
    ('41378', N'LA ARGENTINA'),
    ('41396', N'LA PLATA'),
    ('41483', N'NATAGA'),
    ('41503', N'OPORAPA'),
    ('41518', N'PAICOL'),
    ('41524', N'PALERMO'),
    ('41530', N'PALESTINA'),
    ('41548', N'PITAL'),
    ('41551', N'PITALITO'),
    ('41615', N'RIVERA'),
    ('41660', N'SALADOBLANCO'),
    ('41668', N'SAN AGUSTIN'),
    ('41676', N'SANTA MARIA'),
    ('41770', N'SUAZA'),
    ('41791', N'TARQUI'),
    ('41797', N'TESALIA'),
    ('41799', N'TELLO'),
    ('41801', N'TERUEL'),
    ('41807', N'TIMANA'),
    ('41872', N'VILLAVIEJA'),
    ('41885', N'YAGUARA'),
    -- LA GUAJIRA
    ('44001', N'RIOHACHA'),
    ('44035', N'ALBANIA'),
    ('44078', N'BARRANCAS'),
    ('44090', N'DIBULLA'),
    ('44098', N'DISTRACCION'),
    ('44110', N'EL MOLINO'),
    ('44279', N'FONSECA'),
    ('44378', N'HATONUEVO'),
    ('44420', N'LA JAGUA DEL PILAR'),
    ('44430', N'MAICAO'),
    ('44560', N'MANAURE'),
    ('44650', N'SAN JUAN DEL CESAR'),
    ('44847', N'URIBIA'),
    ('44855', N'URUMITA'),
    ('44874', N'VILLANUEVA'),
    -- MAGDALENA
    ('47001', N'SANTA MARTA'),
    ('47030', N'ALGARROBO'),
    ('47053', N'ARACATACA'),
    ('47058', N'ARIGUANI'),
    ('47161', N'CERRO SAN ANTONIO'),
    ('47170', N'CHIBOLO'),
    ('47189', N'CIENAGA'),
    ('47205', N'CONCORDIA'),
    ('47245', N'EL BANCO'),
    ('47258', N'EL PIÑON'),
    ('47268', N'EL RETEN'),
    ('47288', N'FUNDACION'),
    ('47318', N'GUAMAL'),
    ('47460', N'NUEVA GRANADA'),
    ('47541', N'PEDRAZA'),
    ('47545', N'PIJIÑO DEL CARMEN'),
    ('47551', N'PIVIJAY'),
    ('47555', N'PLATO'),
    ('47570', N'PUEBLOVIEJO'),
    ('47605', N'REMOLINO'),
    ('47660', N'SABANAS DE SAN ANGEL'),
    ('47675', N'SALAMINA'),
    ('47692', N'SAN SEBASTIAN DE BUENAVISTA'),
    ('47703', N'SAN ZENON'),
    ('47707', N'SANTA ANA'),
    ('47720', N'SANTA BARBARA DE PINTO'),
    ('47745', N'SITIONUEVO'),
    ('47798', N'TENERIFE'),
    ('47960', N'ZAPAYAN'),
    ('47980', N'ZONA BANANERA'),
    -- META
    ('50001', N'VILLAVICENCIO'),
    ('50006', N'ACACIAS'),
    ('50110', N'BARRANCA DE UPIA'),
    ('50124', N'CABUYARO'),
    ('50150', N'CASTILLA LA NUEVA'),
    ('50223', N'CUBARRAL'),
    ('50226', N'CUMARAL'),
    ('50245', N'EL CALVARIO'),
    ('50251', N'EL CASTILLO'),
    ('50270', N'EL DORADO'),
    ('50287', N'FUENTE DE ORO'),
    ('50313', N'GRANADA'),
    ('50318', N'GUAMAL'),
    ('50325', N'MAPIRIPAN'),
    ('50330', N'MESETAS'),
    ('50350', N'LA MACARENA'),
    ('50370', N'URIBE'),
    ('50400', N'LEJANIAS'),
    ('50450', N'PUERTO CONCORDIA'),
    ('50568', N'PUERTO GAITAN'),
    ('50573', N'PUERTO LOPEZ'),
    ('50577', N'PUERTO LLERAS'),
    ('50590', N'PUERTO RICO'),
    ('50606', N'RESTREPO'),
    ('50680', N'SAN CARLOS DE GUAROA'),
    ('50683', N'SAN JUAN DE ARAMA'),
    ('50686', N'SAN JUANITO'),
    ('50689', N'SAN MARTIN'),
    ('50711', N'VISTAHERMOSA'),
    -- NARIÑO
    ('52001', N'PASTO'),
    ('52019', N'ALBAN'),
    ('52022', N'ALDANA'),
    ('52036', N'ANCUYA'),
    ('52051', N'ARBOLEDA'),
    ('52079', N'BARBACOAS'),
    ('52083', N'BELEN'),
    ('52110', N'BUESACO'),
    ('52203', N'COLON'),
    ('52207', N'CONSACA'),
    ('52210', N'CONTADERO'),
    ('52215', N'CORDOBA'),
    ('52224', N'CUASPUD'),
    ('52227', N'CUMBAL'),
    ('52233', N'CUMBITARA'),
    ('52240', N'CHACHAGUI'),
    ('52250', N'EL CHARCO'),
    ('52254', N'EL PEÑOL'),
    ('52256', N'EL ROSARIO'),
    ('52258', N'EL TABLON DE GOMEZ'),
    ('52260', N'EL TAMBO'),
    ('52287', N'FUNES'),
    ('52317', N'GUACHUCAL'),
    ('52320', N'GUAITARILLA'),
    ('52323', N'GUALMATAN'),
    ('52352', N'ILES'),
    ('52354', N'IMUES'),
    ('52356', N'IPIALES'),
    ('52378', N'LA CRUZ'),
    ('52381', N'LA FLORIDA'),
    ('52385', N'LA LLANADA'),
    ('52390', N'LA TOLA'),
    ('52399', N'LA UNION'),
    ('52405', N'LEIVA'),
    ('52411', N'LINARES'),
    ('52418', N'LOS ANDES'),
    ('52427', N'MAGUI'),
    ('52435', N'MALLAMA'),
    ('52473', N'MOSQUERA'),
    ('52480', N'NARIÑO'),
    ('52490', N'OLAYA HERRERA'),
    ('52506', N'OSPINA'),
    ('52520', N'FRANCISCO PIZARRO'),
    ('52540', N'POLICARPA'),
    ('52560', N'POTOSI'),
    ('52565', N'PROVIDENCIA'),
    ('52573', N'PUERRES'),
    ('52585', N'PUPIALES'),
    ('52612', N'RICAURTE'),
    ('52621', N'ROBERTO PAYAN'),
    ('52678', N'SAMANIEGO'),
    ('52683', N'SANDONA'),
    ('52685', N'SAN BERNARDO'),
    ('52687', N'SAN LORENZO'),
    ('52693', N'SAN PABLO'),
    ('52694', N'SAN PEDRO DE CARTAGO'),
    ('52696', N'SANTA BARBARA'),
    ('52699', N'SANTACRUZ'),
    ('52720', N'SAPUYES'),
    ('52786', N'TAMINANGO'),
    ('52788', N'TANGUA'),
    ('52835', N'SAN ANDRES DE TUMACO'),
    ('52838', N'TUQUERRES'),
    ('52885', N'YACUANQUER'),
    -- NORTE DE SANTANDER
    ('54001', N'CUCUTA'),
    ('54003', N'ABREGO'),
    ('54051', N'ARBOLEDAS'),
    ('54099', N'BOCHALEMA'),
    ('54109', N'BUCARASICA'),
    ('54125', N'CACOTA'),
    ('54128', N'CACHIRA'),
    ('54172', N'CHINACOTA'),
    ('54174', N'CHITAGA'),
    ('54206', N'CONVENCION'),
    ('54223', N'CUCUTILLA'),
    ('54239', N'DURANIA'),
    ('54245', N'EL CARMEN'),
    ('54250', N'EL TARRA'),
    ('54261', N'EL ZULIA'),
    ('54313', N'GRAMALOTE'),
    ('54344', N'HACARI'),
    ('54347', N'HERRAN'),
    ('54377', N'LABATECA'),
    ('54385', N'LA ESPERANZA'),
    ('54398', N'LA PLAYA'),
    ('54405', N'LOS PATIOS'),
    ('54418', N'LOURDES'),
    ('54480', N'MUTISCUA'),
    ('54498', N'OCAÑA'),
    ('54518', N'PAMPLONA'),
    ('54520', N'PAMPLONITA'),
    ('54553', N'PUERTO SANTANDER'),
    ('54599', N'RAGONVALIA'),
    ('54660', N'SALAZAR'),
    ('54670', N'SAN CALIXTO'),
    ('54673', N'SAN CAYETANO'),
    ('54680', N'SANTIAGO'),
    ('54720', N'SARDINATA'),
    ('54743', N'SILOS'),
    ('54800', N'TEORAMA'),
    ('54810', N'TIBU'),
    ('54820', N'TOLEDO'),
    ('54871', N'VILLA CARO'),
    ('54874', N'VILLA DEL ROSARIO'),
    -- QUINDIO
    ('63001', N'ARMENIA'),
    ('63111', N'BUENAVISTA'),
    ('63130', N'CALARCA'),
    ('63190', N'CIRCASIA'),
    ('63212', N'CORDOBA'),
    ('63272', N'FILANDIA'),
    ('63302', N'GENOVA'),
    ('63401', N'LA TEBAIDA'),
    ('63470', N'MONTENEGRO'),
    ('63548', N'PIJAO'),
    ('63594', N'QUIMBAYA'),
    ('63690', N'SALENTO'),
    -- RISARALDA
    ('66001', N'PEREIRA'),
    ('66045', N'APIA'),
    ('66075', N'BALBOA'),
    ('66088', N'BELEN DE UMBRIA'),
    ('66170', N'DOSQUEBRADAS'),
    ('66318', N'GUATICA'),
    ('66383', N'LA CELIA'),
    ('66400', N'LA VIRGINIA'),
    ('66440', N'MARSELLA'),
    ('66456', N'MISTRATO'),
    ('66572', N'PUEBLO RICO'),
    ('66594', N'QUINCHIA'),
    ('66682', N'SANTA ROSA DE CABAL'),
    ('66687', N'SANTUARIO'),
    -- SANTANDER
    ('68001', N'BUCARAMANGA'),
    ('68013', N'AGUADA'),
    ('68020', N'ALBANIA'),
    ('68051', N'ARATOCA'),
    ('68077', N'BARBOSA'),
    ('68079', N'BARICHARA'),
    ('68081', N'BARRANCABERMEJA'),
    ('68092', N'BETULIA'),
    ('68101', N'BOLIVAR'),
    ('68121', N'CABRERA'),
    ('68132', N'CALIFORNIA'),
    ('68147', N'CAPITANEJO'),
    ('68152', N'CARCASI'),
    ('68160', N'CEPITA'),
    ('68162', N'CERRITO'),
    ('68167', N'CHARALA'),
    ('68169', N'CHARTA'),
    ('68176', N'CHIMA'),
    ('68179', N'CHIPATA'),
    ('68190', N'CIMITARRA'),
    ('68207', N'CONCEPCION'),
    ('68209', N'CONFINES'),
    ('68211', N'CONTRATACION'),
    ('68217', N'COROMORO'),
    ('68229', N'CURITI'),
    ('68235', N'EL CARMEN DE CHUCURI'),
    ('68245', N'EL GUACAMAYO'),
    ('68250', N'EL PEÑON'),
    ('68255', N'EL PLAYON'),
    ('68264', N'ENCINO'),
    ('68266', N'ENCISO'),
    ('68271', N'FLORIAN'),
    ('68276', N'FLORIDABLANCA'),
    ('68296', N'GALAN'),
    ('68298', N'GAMBITA'),
    ('68307', N'GIRON'),
    ('68318', N'GUACA'),
    ('68320', N'GUADALUPE'),
    ('68322', N'GUAPOTA'),
    ('68324', N'GUAVATA'),
    ('68327', N'GUEPSA'),
    ('68344', N'HATO'),
    ('68368', N'JESUS MARIA'),
    ('68370', N'JORDAN'),
    ('68377', N'LA BELLEZA'),
    ('68385', N'LANDAZURI'),
    ('68397', N'LA PAZ'),
    ('68406', N'LEBRIJA'),
    ('68418', N'LOS SANTOS'),
    ('68425', N'MACARAVITA'),
    ('68432', N'MALAGA'),
    ('68444', N'MATANZA'),
    ('68464', N'MOGOTES'),
    ('68468', N'MOLAGAVITA'),
    ('68498', N'OCAMONTE'),
    ('68500', N'OIBA'),
    ('68502', N'ONZAGA'),
    ('68522', N'PALMAR'),
    ('68524', N'PALMAS DEL SOCORRO'),
    ('68533', N'PARAMO'),
    ('68547', N'PIEDECUESTA'),
    ('68549', N'PINCHOTE'),
    ('68572', N'PUENTE NACIONAL'),
    ('68573', N'PUERTO PARRA'),
    ('68575', N'PUERTO WILCHES'),
    ('68615', N'RIONEGRO'),
    ('68655', N'SABANA DE TORRES'),
    ('68669', N'SAN ANDRES'),
    ('68673', N'SAN BENITO'),
    ('68679', N'SAN GIL'),
    ('68682', N'SAN JOAQUIN'),
    ('68684', N'SAN JOSE DE MIRANDA'),
    ('68686', N'SAN MIGUEL'),
    ('68689', N'SAN VICENTE DE CHUCURI'),
    ('68705', N'SANTA BARBARA'),
    ('68720', N'SANTA HELENA DEL OPON'),
    ('68745', N'SIMACOTA'),
    ('68755', N'SOCORRO'),
    ('68770', N'SUAITA'),
    ('68773', N'SUCRE'),
    ('68780', N'SURATA'),
    ('68820', N'TONA'),
    ('68855', N'VALLE DE SAN JOSE'),
    ('68861', N'VELEZ'),
    ('68867', N'VETAS'),
    ('68872', N'VILLANUEVA'),
    ('68895', N'ZAPATOCA'),
    -- SUCRE
    ('70001', N'SINCELEJO'),
    ('70110', N'BUENAVISTA'),
    ('70124', N'CAIMITO'),
    ('70204', N'COLOSO'),
    ('70215', N'COROZAL'),
    ('70221', N'COVEÑAS'),
    ('70230', N'CHALAN'),
    ('70233', N'EL ROBLE'),
    ('70235', N'GALERAS'),
    ('70265', N'GUARANDA'),
    ('70400', N'LA UNION'),
    ('70418', N'LOS PALMITOS'),
    ('70429', N'MAJAGUAL'),
    ('70473', N'MORROA'),
    ('70508', N'OVEJAS'),
    ('70523', N'PALMITO'),
    ('70670', N'SAMPUES'),
    ('70678', N'SAN BENITO ABAD'),
    ('70702', N'SAN JUAN DE BETULIA'),
    ('70708', N'SAN MARCOS'),
    ('70713', N'SAN ONOFRE'),
    ('70717', N'SAN PEDRO'),
    ('70742', N'SAN LUIS DE SINCE'),
    ('70771', N'SUCRE'),
    ('70820', N'SANTIAGO DE TOLU'),
    ('70823', N'TOLU VIEJO'),
    -- TOLIMA
    ('73001', N'IBAGUE'),
    ('73024', N'ALPUJARRA'),
    ('73026', N'ALVARADO'),
    ('73030', N'AMBALEMA'),
    ('73043', N'ANZOATEGUI'),
    ('73055', N'ARMERO'),
    ('73067', N'ATACO'),
    ('73124', N'CAJAMARCA'),
    ('73148', N'CARMEN DE APICALA'),
    ('73152', N'CASABIANCA'),
    ('73168', N'CHAPARRAL'),
    ('73200', N'COELLO'),
    ('73217', N'COYAIMA'),
    ('73226', N'CUNDAY'),
    ('73236', N'DOLORES'),
    ('73268', N'ESPINAL'),
    ('73270', N'FALAN'),
    ('73275', N'FLANDES'),
    ('73283', N'FRESNO'),
    ('73319', N'GUAMO'),
    ('73347', N'HERVEO'),
    ('73349', N'HONDA'),
    ('73352', N'ICONONZO'),
    ('73408', N'LERIDA'),
    ('73411', N'LIBANO'),
    ('73443', N'MARIQUITA'),
    ('73449', N'MELGAR'),
    ('73461', N'MURILLO'),
    ('73483', N'NATAGAIMA'),
    ('73504', N'ORTEGA'),
    ('73520', N'PALOCABILDO'),
    ('73547', N'PIEDRAS'),
    ('73555', N'PLANADAS'),
    ('73563', N'PRADO'),
    ('73585', N'PURIFICACION'),
    ('73616', N'RIOBLANCO'),
    ('73622', N'RONCESVALLES'),
    ('73624', N'ROVIRA'),
    ('73671', N'SALDAÑA'),
    ('73675', N'SAN ANTONIO'),
    ('73678', N'SAN LUIS'),
    ('73686', N'SANTA ISABEL'),
    ('73770', N'SUAREZ'),
    ('73854', N'VALLE DE SAN JUAN'),
    ('73861', N'VENADILLO'),
    ('73870', N'VILLAHERMOSA'),
    ('73873', N'VILLARRICA'),
    -- VALLE DEL CAUCA
    ('76001', N'CALI'),
    ('76020', N'ALCALA'),
    ('76036', N'ANDALUCIA'),
    ('76041', N'ANSERMANUEVO'),
    ('76054', N'ARGELIA'),
    ('76100', N'BOLIVAR'),
    ('76109', N'BUENAVENTURA'),
    ('76111', N'GUADALAJARA DE BUGA'),
    ('76113', N'BUGALAGRANDE'),
    ('76122', N'CAICEDONIA'),
    ('76126', N'CALIMA'),
    ('76130', N'CANDELARIA'),
    ('76147', N'CARTAGO'),
    ('76233', N'DAGUA'),
    ('76243', N'EL AGUILA'),
    ('76246', N'EL CAIRO'),
    ('76248', N'EL CERRITO'),
    ('76250', N'EL DOVIO'),
    ('76275', N'FLORIDA'),
    ('76306', N'GINEBRA'),
    ('76318', N'GUACARI'),
    ('76364', N'JAMUNDI'),
    ('76377', N'LA CUMBRE'),
    ('76400', N'LA UNION'),
    ('76403', N'LA VICTORIA'),
    ('76497', N'OBANDO'),
    ('76520', N'PALMIRA'),
    ('76563', N'PRADERA'),
    ('76606', N'RESTREPO'),
    ('76616', N'RIOFRIO'),
    ('76622', N'ROLDANILLO'),
    ('76670', N'SAN PEDRO'),
    ('76736', N'SEVILLA'),
    ('76823', N'TORO'),
    ('76828', N'TRUJILLO'),
    ('76834', N'TULUA'),
    ('76845', N'ULLOA'),
    ('76863', N'VERSALLES'),
    ('76869', N'VIJES'),
    ('76890', N'YOTOCO'),
    ('76892', N'YUMBO'),
    ('76895', N'ZARZAL'),
    -- ARAUCA
    ('81001', N'ARAUCA'),
    ('81065', N'ARAUQUITA'),
    ('81220', N'CRAVO NORTE'),
    ('81300', N'FORTUL'),
    ('81591', N'PUERTO RONDON'),
    ('81736', N'SARAVENA'),
    ('81794', N'TAME'),
    -- CASANARE
    ('85001', N'YOPAL'),
    ('85010', N'AGUAZUL'),
    ('85015', N'CHAMEZA'),
    ('85125', N'HATO COROZAL'),
    ('85136', N'LA SALINA'),
    ('85139', N'MANI'),
    ('85162', N'MONTERREY'),
    ('85225', N'NUNCHIA'),
    ('85230', N'OROCUE'),
    ('85250', N'PAZ DE ARIPORO'),
    ('85263', N'PORE'),
    ('85279', N'RECETOR'),
    ('85300', N'SABANALARGA'),
    ('85315', N'SACAMA'),
    ('85325', N'SAN LUIS DE PALENQUE'),
    ('85400', N'TAMARA'),
    ('85410', N'TAURAMENA'),
    ('85430', N'TRINIDAD'),
    ('85440', N'VILLANUEVA'),
    -- PUTUMAYO
    ('86001', N'MOCOA'),
    ('86219', N'COLON'),
    ('86320', N'ORITO'),
    ('86568', N'PUERTO ASIS'),
    ('86569', N'PUERTO CAICEDO'),
    ('86571', N'PUERTO GUZMAN'),
    ('86573', N'LEGUIZAMO'),
    ('86749', N'SIBUNDOY'),
    ('86755', N'SAN FRANCISCO'),
    ('86757', N'SAN MIGUEL'),
    ('86760', N'SANTIAGO'),
    ('86865', N'VALLE DEL GUAMUEZ'),
    ('86885', N'VILLAGARZON'),
    -- SAN ANDRES, PROVIDENCIA Y SANTA CATALINA
    ('88001', N'SAN ANDRES'),
    ('88564', N'PROVIDENCIA'),
    -- AMAZONAS
    ('91001', N'LETICIA'),
    ('91263', N'EL ENCANTO'),
    ('91405', N'LA CHORRERA'),
    ('91407', N'LA PEDRERA'),
    ('91430', N'LA VICTORIA'),
    ('91460', N'MIRITI - PARANA'),
    ('91530', N'PUERTO ALEGRIA'),
    ('91536', N'PUERTO ARICA'),
    ('91540', N'PUERTO NARIÑO'),
    ('91669', N'PUERTO SANTANDER'),
    ('91798', N'TARAPACA'),
    -- GUAINIA
    ('94001', N'INIRIDA'),
    ('94343', N'BARRANCO MINAS'),
    ('94663', N'MAPIRIPANA'),
    ('94883', N'SAN FELIPE'),
    ('94884', N'PUERTO COLOMBIA'),
    ('94885', N'LA GUADALUPE'),
    ('94886', N'CACAHUAL'),
    ('94887', N'PANA PANA'),
    ('94888', N'MORICHAL'),
    -- GUAVIARE
    ('95001', N'SAN JOSE DEL GUAVIARE'),
    ('95015', N'CALAMAR'),
    ('95025', N'EL RETORNO'),
    ('95200', N'MIRAFLORES'),
    -- VAUPES
    ('97001', N'MITU'),
    ('97161', N'CARURU'),
    ('97511', N'PACOA'),
    ('97666', N'TARAIRA'),
    ('97777', N'PAPUNAUA'),
    ('97889', N'YAVARATE'),
    -- VICHADA
    ('99001', N'PUERTO CARREÑO'),
    ('99524', N'LA PRIMAVERA'),
    ('99624', N'SANTA ROSALIA'),
    ('99773', N'CUMARIBO')
) AS m (CodigoDane, Nombre)
JOIN Departamentos AS d ON d.CodigoDane = LEFT(m.CodigoDane, 2);
GO

/* ============================================================
   2. USUARIOS  (CU-01, CU-10, CU-24, CU-25)
   ============================================================ */
CREATE TABLE Usuarios (
    UsuarioID            INT IDENTITY(1,1) PRIMARY KEY,
    NombreCompleto       NVARCHAR(150) NOT NULL,
    NumeroDocumento      NVARCHAR(30)  NOT NULL UNIQUE,   -- RN-1-CU01
    Correo               NVARCHAR(150) NOT NULL UNIQUE,   -- RN-2-CU01
    ContrasenaHash       NVARCHAR(256) NOT NULL,          -- nunca texto plano (RNF-01)
    Telefono             NVARCHAR(30)  NULL,
    Direccion            NVARCHAR(200) NULL,
    FotoPerfilUrl        NVARCHAR(300) NULL,
    RolID                INT NOT NULL,
    -- CORRECCIÓN 1: el estado sale del catálogo (antes: Activo BIT)
    EstadoID             INT NOT NULL DEFAULT 1,          -- 1 = Activo, 2 = Inactivo (CU-10)
    AmbitoEstado         AS CAST('USUARIO' AS VARCHAR(20)) PERSISTED,
    -- El bloqueo temporal de 15 min no es un estado: se controla por tiempo
    IntentosFallidos     INT NOT NULL DEFAULT 0,          -- RN-5-CU01
    BloqueadoHasta       DATETIME2 NULL,
    FechaRegistro        DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaActualizacion   DATETIME2 NULL,
    CONSTRAINT FK_Usuarios_Roles   FOREIGN KEY (RolID) REFERENCES Roles(RolID),
    CONSTRAINT FK_Usuarios_Estados FOREIGN KEY (EstadoID, AmbitoEstado)
        REFERENCES Estados(EstadoID, Ambito)
);
GO

CREATE INDEX IX_Usuarios_RolID ON Usuarios(RolID);
GO

/* Tokens de recuperación de contraseña (CU-23) - sin cambios */
CREATE TABLE TokensRecuperacion (
    TokenID          INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioID        INT NOT NULL,
    Token            NVARCHAR(200) NOT NULL UNIQUE,
    FechaExpiracion  DATETIME2 NOT NULL,                  -- RN-1-CU23 (ej. 30 min)
    Usado            BIT NOT NULL DEFAULT 0,
    FechaCreacion    DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_TokensRecuperacion_Usuarios FOREIGN KEY (UsuarioID)
        REFERENCES Usuarios(UsuarioID) ON DELETE CASCADE
);
GO

/* Auditoría de acciones del Administrador sobre usuarios (CU-10, RN-5-CU10) - sin cambios */
CREATE TABLE AuditoriaUsuarios (
    AuditoriaID          INT IDENTITY(1,1) PRIMARY KEY,
    AdministradorID      INT NOT NULL,
    UsuarioAfectadoID    INT NOT NULL,
    Accion               NVARCHAR(30) NOT NULL
        CHECK (Accion IN ('EditarDatos','CambiarRol','Activar','Desactivar')),
    Detalle              NVARCHAR(300) NULL,
    FechaAccion          DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_AuditoriaUsuarios_Admin FOREIGN KEY (AdministradorID)
        REFERENCES Usuarios(UsuarioID),
    CONSTRAINT FK_AuditoriaUsuarios_Afectado FOREIGN KEY (UsuarioAfectadoID)
        REFERENCES Usuarios(UsuarioID)
);
GO

/* ============================================================
   3. VACAS  (CU-02, CU-12, CU-13, CU-14)
   ============================================================ */
CREATE TABLE Vacas (
    VacaID              INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioID           INT NOT NULL,                      -- RN-3-CU02 dueño
    Identificacion      NVARCHAR(50) NOT NULL UNIQUE,      -- arete/chip, RN-1-CU02
    Raza                NVARCHAR(80) NOT NULL,
    Sexo                NVARCHAR(10) NOT NULL CHECK (Sexo IN ('Macho','Hembra')),
    FechaNacimiento     DATE NULL,
    Peso                DECIMAL(10,2) NOT NULL CHECK (Peso > 0),
    FotoUrl             NVARCHAR(300) NULL,
    -- CORRECCIÓN 7: municipio de origen de la vaca (opcional)
    MunicipioOrigenID   INT NULL,
    -- CORRECCIÓN 1: el estado sale del catálogo
    EstadoID            INT NOT NULL DEFAULT 10,           -- 10 = Viva
    AmbitoEstado        AS CAST('VACA' AS VARCHAR(20)) PERSISTED,
    -- CORRECCIÓN 2: datos de la muerte (CU-13)
    FechaMuerte         DATE NULL,
    CausaMuerteID       INT NULL,                          -- causa, del catálogo CausasMuerte
    ComentarioMuerte    NVARCHAR(300) NULL,                -- detalle opcional
    FechaRegistro       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaActualizacion  DATETIME2 NULL,
    CONSTRAINT FK_Vacas_Usuarios     FOREIGN KEY (UsuarioID) REFERENCES Usuarios(UsuarioID),
    CONSTRAINT FK_Vacas_Municipios   FOREIGN KEY (MunicipioOrigenID) REFERENCES Municipios(MunicipioID),
    CONSTRAINT FK_Vacas_Estados      FOREIGN KEY (EstadoID, AmbitoEstado)
        REFERENCES Estados(EstadoID, Ambito),
    CONSTRAINT FK_Vacas_CausasMuerte FOREIGN KEY (CausaMuerteID) REFERENCES CausasMuerte(CausaMuerteID),
    CONSTRAINT CK_Vacas_FechaMuerte CHECK (FechaMuerte IS NULL OR FechaMuerte <= CAST(SYSDATETIME() AS DATE)),
    -- Si está Fallecida (11) debe tener fecha y causa; si no, no lleva datos de muerte
    CONSTRAINT CK_Vacas_DatosMuerte CHECK (
        (EstadoID = 11 AND FechaMuerte IS NOT NULL AND CausaMuerteID IS NOT NULL)
        OR
        (EstadoID <> 11 AND FechaMuerte IS NULL AND CausaMuerteID IS NULL AND ComentarioMuerte IS NULL)
    )
);
GO

CREATE INDEX IX_Vacas_UsuarioID ON Vacas(UsuarioID);
GO

/* Venta fuera de la plataforma (CU-14) - una por vaca.
   CompradorNombre se deja como texto opcional porque en una venta
   externa el comprador normalmente NO es usuario de la plataforma
   (no tiene ID). */
CREATE TABLE VentasExternas (
    VentaExternaID     INT IDENTITY(1,1) PRIMARY KEY,
    VacaID             INT NOT NULL UNIQUE,
    FechaVenta         DATE NOT NULL,
    CompradorNombre    NVARCHAR(150) NULL,                 -- opcional, RN-2-CU14
    ValorVenta         DECIMAL(12,2) NULL,
    MunicipioVentaID   INT NULL,                           -- CORRECCIÓN 7: origen de la venta
    FechaRegistro      DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_VentasExternas_Vacas      FOREIGN KEY (VacaID) REFERENCES Vacas(VacaID),
    CONSTRAINT FK_VentasExternas_Municipios FOREIGN KEY (MunicipioVentaID) REFERENCES Municipios(MunicipioID)
);
GO

/* ============================================================
   4. PUBLICACIONES DE VACA EN VENTA  (CU-03, CU-16, CU-17)
   ============================================================ */
CREATE TABLE PublicacionesVaca (
    PublicacionVacaID   INT IDENTITY(1,1) PRIMARY KEY,
    VacaID              INT NOT NULL,
    Precio              DECIMAL(12,2) NOT NULL CHECK (Precio > 0),   -- RN-2-CU03
    Descripcion         NVARCHAR(MAX) NOT NULL,
    MunicipioID         INT NOT NULL,                     -- CORRECCIÓN 7 (antes: Ubicacion en texto)
    FotoUrl             NVARCHAR(300) NOT NULL,           -- RN-4-CU03
    EstadoID            INT NOT NULL DEFAULT 20,          -- CORRECCIÓN 1: 20 = Activa
    AmbitoEstado        AS CAST('PUBLICACION' AS VARCHAR(20)) PERSISTED,
    MotivoRechazo       NVARCHAR(300) NULL,               -- CU-09
    FechaPublicacion    DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaActualizacion  DATETIME2 NULL,
    CONSTRAINT FK_PublicacionesVaca_Vacas      FOREIGN KEY (VacaID) REFERENCES Vacas(VacaID),
    CONSTRAINT FK_PublicacionesVaca_Municipios FOREIGN KEY (MunicipioID) REFERENCES Municipios(MunicipioID),
    CONSTRAINT FK_PublicacionesVaca_Estados    FOREIGN KEY (EstadoID, AmbitoEstado)
        REFERENCES Estados(EstadoID, Ambito)
);
GO

CREATE INDEX IX_PublicacionesVaca_VacaID ON PublicacionesVaca(VacaID);
GO

-- RN-3-CU03: una vaca no puede tener dos publicaciones activas al mismo tiempo (20 = Activa)
CREATE UNIQUE INDEX UX_PublicacionesVaca_UnaActivaPorVaca
    ON PublicacionesVaca(VacaID)
    WHERE EstadoID = 20;
GO

/* ============================================================
   5. PUBLICACIONES DE LOTE DE PASTO  (CU-04, CU-18, CU-19, CU-20)
   ============================================================ */
CREATE TABLE PublicacionesLotePasto (
    PublicacionLoteID   INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioID           INT NOT NULL,
    Area                DECIMAL(10,2) NOT NULL CHECK (Area > 0),      -- hectáreas, RN-1-CU04
    TipoPasto           NVARCHAR(80) NULL,
    TipoPublicacionID   INT NOT NULL,                     -- CORRECCIÓN 8: Venta / Arriendo
    Precio              DECIMAL(12,2) NOT NULL CHECK (Precio > 0),
    MunicipioID         INT NOT NULL,                     -- CORRECCIÓN 7 (antes: Departamento y Municipio en texto)
    Descripcion         NVARCHAR(MAX) NULL,
    FotoUrl             NVARCHAR(300) NOT NULL,
    EstadoID            INT NOT NULL DEFAULT 20,          -- CORRECCIÓN 1: 20 = Activa
    AmbitoEstado        AS CAST('PUBLICACION' AS VARCHAR(20)) PERSISTED,
    MotivoRechazo       NVARCHAR(300) NULL,
    FechaPublicacion    DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaActualizacion  DATETIME2 NULL,
    CONSTRAINT FK_PublicacionesLote_Usuarios         FOREIGN KEY (UsuarioID) REFERENCES Usuarios(UsuarioID),
    CONSTRAINT FK_PublicacionesLote_TiposPublicacion FOREIGN KEY (TipoPublicacionID)
        REFERENCES TiposPublicacion(TipoPublicacionID),
    CONSTRAINT FK_PublicacionesLote_Municipios       FOREIGN KEY (MunicipioID) REFERENCES Municipios(MunicipioID),
    CONSTRAINT FK_PublicacionesLote_Estados          FOREIGN KEY (EstadoID, AmbitoEstado)
        REFERENCES Estados(EstadoID, Ambito)
);
GO

CREATE INDEX IX_PublicacionesLote_UsuarioID ON PublicacionesLotePasto(UsuarioID);
GO

/* Arriendo/venta de lote (CU-20) - un registro por publicación
   CORRECCIÓN 5: en vez del nombre del arrendatario/comprador, solo su ID.
   CORRECCIÓN 6: el comprador no puede ser el mismo vendedor. */
CREATE TABLE ArriendosLote (
    ArriendoID           INT IDENTITY(1,1) PRIMARY KEY,
    PublicacionLoteID    INT NOT NULL UNIQUE,
    VendedorID           INT NOT NULL,                    -- dueño del lote (quien publicó)
    CompradorID          INT NULL,                        -- arrendatario o comprador (opcional)
    FechaArriendoVenta   DATE NOT NULL,
    Valor                DECIMAL(12,2) NULL,
    FechaRegistro        DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_ArriendosLote_Publicacion FOREIGN KEY (PublicacionLoteID)
        REFERENCES PublicacionesLotePasto(PublicacionLoteID),
    CONSTRAINT FK_ArriendosLote_Vendedor  FOREIGN KEY (VendedorID)  REFERENCES Usuarios(UsuarioID),
    CONSTRAINT FK_ArriendosLote_Comprador FOREIGN KEY (CompradorID) REFERENCES Usuarios(UsuarioID),
    CONSTRAINT CK_ArriendosLote_CompradorDistintoVendedor CHECK (CompradorID <> VendedorID)
);
GO

/* ============================================================
   6. REGISTROS SANITARIOS: VACUNAS Y TRATAMIENTOS  (CU-07, CU-08, CU-21)
   Antes se llamaba VacunasTratamientos.
   CORRECCIÓN 3: el tipo (Vacuna / Tratamiento) sale de un catálogo.
   CORRECCIÓN 4: la anulación se maneja solo con el estado
                 (30 = Activo, 31 = Anulado). Se quitó MotivoAnulacion;
                 si se quiere dejar el motivo, va en Observaciones.
   ============================================================ */
CREATE TABLE RegistrosSanitarios (
    RegistroID         INT IDENTITY(1,1) PRIMARY KEY,
    VacaID             INT NOT NULL,
    TipoRegistroID     INT NOT NULL,                       -- Vacuna / Tratamiento
    Nombre             NVARCHAR(150) NOT NULL,
    FechaAplicacion    DATE NOT NULL,                      -- RN-3-CU07: no puede ser futura
    Responsable        NVARCHAR(150) NOT NULL,
    Observaciones      NVARCHAR(MAX) NULL,
    ProximaDosis       DATE NULL,
    EstadoID           INT NOT NULL DEFAULT 30,            -- 30 = Activo (RN-5-CU07 / RN-3-CU21)
    AmbitoEstado       AS CAST('REGISTRO_SANITARIO' AS VARCHAR(20)) PERSISTED,
    FechaRegistro      DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaActualizacion DATETIME2 NULL,
    CONSTRAINT FK_RegistrosSanitarios_Vacas   FOREIGN KEY (VacaID) REFERENCES Vacas(VacaID),
    CONSTRAINT FK_RegistrosSanitarios_Tipos   FOREIGN KEY (TipoRegistroID)
        REFERENCES TiposRegistroSanitario(TipoRegistroID),
    CONSTRAINT FK_RegistrosSanitarios_Estados FOREIGN KEY (EstadoID, AmbitoEstado)
        REFERENCES Estados(EstadoID, Ambito),
    CONSTRAINT CK_RegistrosSanitarios_FechaAplicacion CHECK (FechaAplicacion <= CAST(SYSDATETIME() AS DATE))
);
GO

CREATE INDEX IX_RegistrosSanitarios_VacaID ON RegistrosSanitarios(VacaID);
GO

/* ============================================================
   7. PRODUCCIÓN DE LECHE  (CU-11, CU-22) - sin cambios
   ============================================================ */
CREATE TABLE ProduccionLeche (
    ProduccionID        INT IDENTITY(1,1) PRIMARY KEY,
    VacaID              INT NOT NULL,
    Fecha               DATE NOT NULL,
    CantidadLitros      DECIMAL(8,2) NOT NULL CHECK (CantidadLitros > 0),
    FechaRegistro       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaActualizacion  DATETIME2 NULL,
    CONSTRAINT FK_ProduccionLeche_Vacas FOREIGN KEY (VacaID) REFERENCES Vacas(VacaID),
    CONSTRAINT CK_ProduccionLeche_Fecha CHECK (Fecha <= CAST(SYSDATETIME() AS DATE)),
    -- RN-5-CU11: solo un registro de producción por vaca por día
    CONSTRAINT UQ_ProduccionLeche_VacaFecha UNIQUE (VacaID, Fecha)
);
GO

CREATE INDEX IX_ProduccionLeche_VacaID ON ProduccionLeche(VacaID);
GO

/* ============================================================
   8. CONTACTOS ENTRE COMPRADOR Y VENDEDOR  (CU-06) - sin cambios
   Puede referirse a una publicación de vaca O de lote, nunca ambas.
   ============================================================ */
CREATE TABLE Contactos (
    ContactoID          INT IDENTITY(1,1) PRIMARY KEY,
    PublicacionVacaID   INT NULL,
    PublicacionLoteID   INT NULL,
    CompradorID         INT NOT NULL,
    Mensaje             NVARCHAR(MAX) NOT NULL,             -- RN-2-CU06: no vacío
    FechaContacto       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Contactos_PublicacionVaca FOREIGN KEY (PublicacionVacaID)
        REFERENCES PublicacionesVaca(PublicacionVacaID),
    CONSTRAINT FK_Contactos_PublicacionLote FOREIGN KEY (PublicacionLoteID)
        REFERENCES PublicacionesLotePasto(PublicacionLoteID),
    CONSTRAINT FK_Contactos_Comprador FOREIGN KEY (CompradorID) REFERENCES Usuarios(UsuarioID),
    CONSTRAINT CK_Contactos_UnaPublicacion CHECK (
        (PublicacionVacaID IS NOT NULL AND PublicacionLoteID IS NULL) OR
        (PublicacionVacaID IS NULL AND PublicacionLoteID IS NOT NULL)
    )
);
GO

CREATE INDEX IX_Contactos_CompradorID ON Contactos(CompradorID);
GO

/* ============================================================
   9. VENTA CONFIRMADA DENTRO DE LA PLATAFORMA  (CU-15)
   CORRECCIÓN 5: se quitaron CompradorNombreManual y
                 CompradorContactoManual; con CompradorID se obtienen
                 todos los datos del comprador.
   CORRECCIÓN 6: el comprador debe ser distinto del vendedor.
   ============================================================ */
CREATE TABLE VentasPlataforma (
    VentaPlataformaID     INT IDENTITY(1,1) PRIMARY KEY,
    PublicacionVacaID     INT NOT NULL UNIQUE,
    VacaID                INT NOT NULL,
    VendedorID            INT NOT NULL,                      -- dueño de la vaca
    CompradorID           INT NOT NULL,                      -- usuario que compró
    FechaVenta            DATE NOT NULL,
    FechaRegistro         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_VentasPlataforma_Publicacion FOREIGN KEY (PublicacionVacaID)
        REFERENCES PublicacionesVaca(PublicacionVacaID),
    CONSTRAINT FK_VentasPlataforma_Vaca      FOREIGN KEY (VacaID)      REFERENCES Vacas(VacaID),
    CONSTRAINT FK_VentasPlataforma_Vendedor  FOREIGN KEY (VendedorID)  REFERENCES Usuarios(UsuarioID),
    CONSTRAINT FK_VentasPlataforma_Comprador FOREIGN KEY (CompradorID) REFERENCES Usuarios(UsuarioID),
    CONSTRAINT CK_VentasPlataforma_FechaVenta CHECK (FechaVenta <= CAST(SYSDATETIME() AS DATE)),
    CONSTRAINT CK_VentasPlataforma_CompradorDistintoVendedor CHECK (CompradorID <> VendedorID)
);
GO

/* ============================================================
   10. MODERACIÓN Y REPORTES DE PUBLICACIONES  (CU-09) - sin cambios
   ============================================================ */
CREATE TABLE ModeracionPublicaciones (
    ModeracionID        INT IDENTITY(1,1) PRIMARY KEY,
    AdministradorID     INT NOT NULL,
    PublicacionVacaID   INT NULL,
    PublicacionLoteID   INT NULL,
    Accion              NVARCHAR(15) NOT NULL CHECK (Accion IN ('Aprobar','Rechazar','Eliminar')),
    Motivo              NVARCHAR(300) NULL,                  -- RN-2-CU09: obligatorio si no es Aprobar
    FechaAccion         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Moderacion_Admin FOREIGN KEY (AdministradorID) REFERENCES Usuarios(UsuarioID),
    CONSTRAINT FK_Moderacion_PublicacionVaca FOREIGN KEY (PublicacionVacaID)
        REFERENCES PublicacionesVaca(PublicacionVacaID),
    CONSTRAINT FK_Moderacion_PublicacionLote FOREIGN KEY (PublicacionLoteID)
        REFERENCES PublicacionesLotePasto(PublicacionLoteID),
    CONSTRAINT CK_Moderacion_UnaPublicacion CHECK (
        (PublicacionVacaID IS NOT NULL AND PublicacionLoteID IS NULL) OR
        (PublicacionVacaID IS NULL AND PublicacionLoteID IS NOT NULL)
    ),
    CONSTRAINT CK_Moderacion_MotivoObligatorio CHECK (
        Accion = 'Aprobar' OR Motivo IS NOT NULL
    )
);
GO

CREATE TABLE ReportesPublicacion (
    ReporteID           INT IDENTITY(1,1) PRIMARY KEY,
    PublicacionVacaID   INT NULL,
    PublicacionLoteID   INT NULL,
    UsuarioReportaID    INT NOT NULL,
    Motivo              NVARCHAR(300) NULL,
    FechaReporte        DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Reportes_PublicacionVaca FOREIGN KEY (PublicacionVacaID)
        REFERENCES PublicacionesVaca(PublicacionVacaID),
    CONSTRAINT FK_Reportes_PublicacionLote FOREIGN KEY (PublicacionLoteID)
        REFERENCES PublicacionesLotePasto(PublicacionLoteID),
    CONSTRAINT FK_Reportes_Usuario FOREIGN KEY (UsuarioReportaID) REFERENCES Usuarios(UsuarioID),
    CONSTRAINT CK_Reportes_UnaPublicacion CHECK (
        (PublicacionVacaID IS NOT NULL AND PublicacionLoteID IS NULL) OR
        (PublicacionVacaID IS NULL AND PublicacionLoteID IS NOT NULL)
    )
);
GO

/* ============================================================
   11. SESIONES  (CU-01 RN-6, CU-23 RN-4) - sin cambios
   Soporte para invalidar tokens al cerrar sesión o cambiar contraseña.
   ============================================================ */
CREATE TABLE Sesiones (
    SesionID            INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioID           INT NOT NULL,
    Token               NVARCHAR(300) NOT NULL UNIQUE,
    FechaInicio         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaExpiracion     DATETIME2 NULL,
    Activa              BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Sesiones_Usuarios FOREIGN KEY (UsuarioID) REFERENCES Usuarios(UsuarioID)
);
GO

CREATE INDEX IX_Sesiones_UsuarioID ON Sesiones(UsuarioID);
GO

/* ============================================================
   12. EJEMPLOS DE CONSULTA (para mostrarle las correcciones al profe)
   Quita los "--" y ejecútalos cuando ya tengas datos.
   ============================================================ */

-- Con el MunicipioID de una publicación se llega al departamento:
-- SELECT p.PublicacionVacaID, m.Nombre AS Municipio, d.Nombre AS Departamento
-- FROM PublicacionesVaca p
-- JOIN Municipios    m ON m.MunicipioID    = p.MunicipioID
-- JOIN Departamentos d ON d.DepartamentoID = m.DepartamentoID;

-- Con el CompradorID de una venta se obtienen todos los datos del comprador:
-- SELECT v.VentaPlataformaID, u.NombreCompleto, u.Correo, u.Telefono
-- FROM VentasPlataforma v
-- JOIN Usuarios u ON u.UsuarioID = v.CompradorID;

-- El estado de cada vaca sale del catálogo centralizado:
-- SELECT va.Identificacion, e.Nombre AS Estado
-- FROM Vacas va
-- JOIN Estados e ON e.EstadoID = va.EstadoID;

/* ============================================================
   13. VERIFICACIÓN: cuántos registros quedaron en cada catálogo
   Debe salir: Roles 3, Estados 15, TiposPublicacion 2,
   TiposRegistroSanitario 2, CausasMuerte 8, Departamentos 33,
   Municipios 1122.
   ============================================================ */
SELECT 'Roles' AS Catalogo, COUNT(*) AS Registros FROM Roles
UNION ALL SELECT 'Estados',                COUNT(*) FROM Estados
UNION ALL SELECT 'TiposPublicacion',       COUNT(*) FROM TiposPublicacion
UNION ALL SELECT 'TiposRegistroSanitario', COUNT(*) FROM TiposRegistroSanitario
UNION ALL SELECT 'CausasMuerte',           COUNT(*) FROM CausasMuerte
UNION ALL SELECT 'Departamentos',          COUNT(*) FROM Departamentos
UNION ALL SELECT 'Municipios',             COUNT(*) FROM Municipios;
GO

/* ============================================================
   FIN DEL SCRIPT
   ============================================================ */

GO

/* ============================================================
   PROYECTO: TU VACA
   Script 02 - Notificaciones (la campanita)

   CÓMO USARLO
   - Ejecútalo DESPUÉS de TuVacaDB_corregido.sql, sobre TuVacaDB.
   - No borra nada: sirve aunque la base ya tenga datos.
   - Si lo ejecutas dos veces no pasa nada: revisa antes de crear.

   CASOS DE USO QUE GENERAN NOTIFICACIONES
   - CU-06 paso 6            : un comprador le escribe al vendedor
   - CU-07 FA-2              : recordatorio de la próxima dosis
   - CU-09 RN-4 y paso 7     : publicación aprobada, rechazada o eliminada
   - CU-13 FA-2 y CU-14 FA-2 : publicación cancelada automáticamente
   - CU-15 RN-6 y paso 7     : venta confirmada (le llega al comprador)
   ============================================================ */

/* ------------------------------------------------------------
   1. ESTADOS DE LA NOTIFICACIÓN (catálogo de estados centralizado)
   Se agrega el ámbito 'NOTIFICACION' a la tabla Estados, con los
   estados No leída (40) y Leída (41).
   ------------------------------------------------------------ */

-- 1.1 Se quita la regla que solo aceptaba los ámbitos anteriores
DECLARE @regla SYSNAME, @sql NVARCHAR(400);

SELECT @regla = name
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('Estados')
  AND definition LIKE '%Ambito%';

IF @regla IS NOT NULL
BEGIN
    SET @sql = N'ALTER TABLE Estados DROP CONSTRAINT ' + QUOTENAME(@regla);
    EXEC sp_executesql @sql;
END
GO

-- 1.2 Nueva regla con el ámbito NOTIFICACION incluido
ALTER TABLE Estados ADD CONSTRAINT CK_Estados_Ambito
    CHECK (Ambito IN ('USUARIO','VACA','PUBLICACION','REGISTRO_SANITARIO','NOTIFICACION'));
GO

-- 1.3 Estados de la notificación
IF NOT EXISTS (SELECT 1 FROM Estados WHERE EstadoID = 40)
    INSERT INTO Estados (EstadoID, Nombre, Ambito) VALUES (40, N'No leída', 'NOTIFICACION');

IF NOT EXISTS (SELECT 1 FROM Estados WHERE EstadoID = 41)
    INSERT INTO Estados (EstadoID, Nombre, Ambito) VALUES (41, N'Leída', 'NOTIFICACION');
GO

/* ------------------------------------------------------------
   2. TIPOS DE NOTIFICACIÓN (catálogo)
   Los ID son fijos porque el código los usa.
   ------------------------------------------------------------ */
IF OBJECT_ID('TiposNotificacion') IS NULL
    CREATE TABLE TiposNotificacion (
        TipoNotificacionID  INT          NOT NULL PRIMARY KEY,
        Nombre              NVARCHAR(60) NOT NULL UNIQUE
    );
GO

IF NOT EXISTS (SELECT 1 FROM TiposNotificacion)
    INSERT INTO TiposNotificacion (TipoNotificacionID, Nombre) VALUES
        (1, N'Nuevo mensaje de un comprador'),          -- CU-06 paso 6
        (2, N'Recordatorio de próxima dosis'),          -- CU-07 FA-2
        (3, N'Publicación aprobada'),                   -- CU-09 RN-4
        (4, N'Publicación rechazada'),                  -- CU-09 RN-4 (con el motivo, RN-2)
        (5, N'Publicación eliminada'),                  -- CU-09 RN-4 (con el motivo, RN-2)
        (6, N'Publicación cancelada automáticamente'),  -- CU-13 FA-2 y CU-14 FA-2
        (7, N'Venta confirmada');                       -- CU-15 RN-6
GO

/* ------------------------------------------------------------
   3. NOTIFICACIONES (lo que muestra la campanita)
   - UsuarioID: quién recibe la notificación.
   - Enlace: página que se abre al hacer clic (opcional).
   - FechaProgramada: desde cuándo se muestra. Normalmente es el
     momento en que se crea. Para el recordatorio de la próxima
     dosis (CU-07 FA-2) se guarda la fecha de esa dosis, y la
     campanita solo lo muestra cuando llega el día.
   - EstadoID: 40 = No leída, 41 = Leída (catálogo Estados).
   - Si el usuario elimina su cuenta (CU-25), sus notificaciones
     se borran con ella (ON DELETE CASCADE).
   ------------------------------------------------------------ */
IF OBJECT_ID('Notificaciones') IS NULL
    CREATE TABLE Notificaciones (
        NotificacionID      INT IDENTITY(1,1) PRIMARY KEY,
        UsuarioID           INT           NOT NULL,
        TipoNotificacionID  INT           NOT NULL,
        Mensaje             NVARCHAR(500) NOT NULL,
        Enlace              NVARCHAR(300) NULL,
        FechaProgramada     DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        EstadoID            INT           NOT NULL DEFAULT 40,   -- 40 = No leída
        AmbitoEstado        AS CAST('NOTIFICACION' AS VARCHAR(20)) PERSISTED,
        FechaCreacion       DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT FK_Notificaciones_Usuarios FOREIGN KEY (UsuarioID)
            REFERENCES Usuarios(UsuarioID) ON DELETE CASCADE,
        CONSTRAINT FK_Notificaciones_TiposNotificacion FOREIGN KEY (TipoNotificacionID)
            REFERENCES TiposNotificacion(TipoNotificacionID),
        CONSTRAINT FK_Notificaciones_Estados FOREIGN KEY (EstadoID, AmbitoEstado)
            REFERENCES Estados(EstadoID, Ambito)
    );
GO

-- Para que la campanita cuente rápido las no leídas de cada usuario
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Notificaciones_Usuario'
                 AND object_id = OBJECT_ID('Notificaciones'))
    CREATE INDEX IX_Notificaciones_Usuario
        ON Notificaciones (UsuarioID, EstadoID, FechaProgramada);
GO

/* ------------------------------------------------------------
   4. VERIFICACIÓN
   Debe salir: TiposNotificacion 7, Estados de notificación 2 y
   Notificaciones 0 (si es la primera vez).
   ------------------------------------------------------------ */
SELECT N'TiposNotificacion' AS Tabla, COUNT(*) AS Registros FROM TiposNotificacion
UNION ALL SELECT N'Estados de notificación', COUNT(*) FROM Estados WHERE Ambito = 'NOTIFICACION'
UNION ALL SELECT N'Notificaciones',          COUNT(*) FROM Notificaciones;
GO

/* ------------------------------------------------------------
   PRUEBA (opcional, para cuando la campanita esté lista)
   Quita los "--" para crearle una notificación al usuario 1.
   ------------------------------------------------------------ */
-- INSERT INTO Notificaciones (UsuarioID, TipoNotificacionID, Mensaje)
-- VALUES (1, 3, N'Tu publicación fue aprobada por el administrador.');

SET NOEXEC OFF;
GO

/* ============================================================
   FIN DEL SCRIPT 02
   ============================================================ */

GO

/* ============================================================
   TABLA DE APOYO: AuditoriaVacas (la llena el TR1)
   ============================================================ */
IF OBJECT_ID('dbo.AuditoriaVacas') IS NULL
BEGIN
    CREATE TABLE dbo.AuditoriaVacas (
        AuditoriaVacaID   INT IDENTITY(1,1) PRIMARY KEY,
        VacaID            INT NOT NULL,
        Campo             NVARCHAR(30) NOT NULL,
        ValorAnterior     NVARCHAR(300) NULL,
        ValorNuevo        NVARCHAR(300) NULL,
        FechaCambio       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT FK_AuditoriaVacas_Vacas FOREIGN KEY (VacaID) REFERENCES dbo.Vacas(VacaID)
    );
    CREATE INDEX IX_AuditoriaVacas_VacaID ON dbo.AuditoriaVacas(VacaID);
END
GO

/* ============================================================
   SP1. sp_BuscarPublicaciones  (CU-05)
   Todos los parámetros son opcionales (NULL = sin ese filtro).
   RN-1: solo publicaciones Activas (20). RN-3: filtros combinables.
   RN-4: @Orden = 'recientes' | 'precio_asc' | 'precio_desc'.
   Ejemplo:
     EXEC dbo.sp_BuscarPublicaciones @Raza = N'Holstein', @PrecioMax = 5000000;
   ============================================================ */
CREATE OR ALTER PROCEDURE dbo.sp_BuscarPublicaciones
    @Texto          NVARCHAR(100) = NULL,
    @DepartamentoID INT           = NULL,
    @MunicipioID    INT           = NULL,
    @PrecioMin      DECIMAL(12,2) = NULL,
    @PrecioMax      DECIMAL(12,2) = NULL,
    @Raza           NVARCHAR(80)  = NULL,
    @EdadMinAnios   INT           = NULL,
    @EdadMaxAnios   INT           = NULL,
    @Orden          VARCHAR(20)   = 'recientes'
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Hoy DATE = CAST(SYSDATETIME() AS DATE);

    SELECT  p.PublicacionVacaID,
            p.Precio,
            p.FotoUrl,
            v.Identificacion,
            v.Raza,
            v.Sexo,
            v.FechaNacimiento,
            DATEDIFF(MONTH, v.FechaNacimiento, @Hoy) / 12 AS EdadAnios,
            v.Peso,
            m.Nombre + N', ' + d.Nombre                  AS Ubicacion,
            u.NombreCompleto                              AS Vendedor,
            p.FechaPublicacion
    FROM dbo.PublicacionesVaca p
    JOIN dbo.Vacas         v ON v.VacaID         = p.VacaID
    JOIN dbo.Usuarios      u ON u.UsuarioID      = v.UsuarioID
    JOIN dbo.Municipios    m ON m.MunicipioID    = p.MunicipioID
    JOIN dbo.Departamentos d ON d.DepartamentoID = m.DepartamentoID
    WHERE p.EstadoID = 20
      AND (@Texto IS NULL OR p.Descripcion LIKE N'%' + @Texto + N'%'
                          OR v.Raza LIKE N'%' + @Texto + N'%'
                          OR v.Identificacion LIKE N'%' + @Texto + N'%'
                          OR m.Nombre LIKE N'%' + @Texto + N'%')
      AND (@MunicipioID    IS NULL OR p.MunicipioID    = @MunicipioID)
      AND (@DepartamentoID IS NULL OR m.DepartamentoID = @DepartamentoID)
      AND (@PrecioMin      IS NULL OR p.Precio        >= @PrecioMin)
      AND (@PrecioMax      IS NULL OR p.Precio        <= @PrecioMax)
      AND (@Raza           IS NULL OR v.Raza           = @Raza)
      AND (@EdadMinAnios   IS NULL OR v.FechaNacimiento <= DATEADD(YEAR, -@EdadMinAnios, @Hoy))
      AND (@EdadMaxAnios   IS NULL OR v.FechaNacimiento >  DATEADD(YEAR, -(@EdadMaxAnios + 1), @Hoy))
    ORDER BY
        CASE WHEN @Orden = 'precio_asc'  THEN p.Precio END ASC,
        CASE WHEN @Orden = 'precio_desc' THEN p.Precio END DESC,
        p.FechaPublicacion DESC;
END
GO

/* ============================================================
   SP2. sp_ReporteProduccion  (CU-11 RN-6)
   Producción de leche de cada vaca hembra de un productor en un
   rango de fechas: días registrados, total, promedio, mínimo y máximo.
   Ejemplo:
     EXEC dbo.sp_ReporteProduccion @UsuarioID = 1, @Desde = '2026-10-01', @Hasta = '2026-11-15';
   ============================================================ */
CREATE OR ALTER PROCEDURE dbo.sp_ReporteProduccion
    @UsuarioID INT,
    @Desde     DATE,
    @Hasta     DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF @Desde > @Hasta
    BEGIN
        DECLARE @tmp DATE = @Desde;
        SET @Desde = @Hasta;
        SET @Hasta = @tmp;
    END

    SELECT  v.VacaID,
            v.Identificacion,
            v.Raza,
            e.Nombre                                        AS Estado,
            COUNT(pl.ProduccionID)                          AS DiasRegistrados,
            CAST(ISNULL(SUM(pl.CantidadLitros), 0) AS DECIMAL(12,2)) AS TotalLitros,
            CAST(ISNULL(AVG(pl.CantidadLitros), 0) AS DECIMAL(12,2)) AS PromedioLitros,
            CAST(ISNULL(MIN(pl.CantidadLitros), 0) AS DECIMAL(12,2)) AS MinimoLitros,
            CAST(ISNULL(MAX(pl.CantidadLitros), 0) AS DECIMAL(12,2)) AS MaximoLitros
    FROM dbo.Vacas v
    JOIN dbo.Estados e ON e.EstadoID = v.EstadoID
    LEFT JOIN dbo.ProduccionLeche pl
           ON pl.VacaID = v.VacaID
          AND pl.Fecha BETWEEN @Desde AND @Hasta
    WHERE v.UsuarioID = @UsuarioID
      AND v.Sexo = N'Hembra'
    GROUP BY v.VacaID, v.Identificacion, v.Raza, e.Nombre
    ORDER BY TotalLitros DESC, v.Identificacion;
END
GO

/* ============================================================
   SP3. sp_HistorialSanitario  (CU-08)
   Historial de vacunas y tratamientos de una vaca, del más reciente
   al más antiguo (RN-2). Solo responde si la vaca es del usuario
   (RN-1). Marca la próxima dosis como Pendiente o Vencida (RN-4).
   Ejemplo:
     EXEC dbo.sp_HistorialSanitario @VacaID = 3, @UsuarioID = 1;
   ============================================================ */
CREATE OR ALTER PROCEDURE dbo.sp_HistorialSanitario
    @VacaID    INT,
    @UsuarioID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Hoy DATE = CAST(SYSDATETIME() AS DATE);

    SELECT  r.RegistroID,
            t.Nombre          AS Tipo,
            r.Nombre,
            r.FechaAplicacion,
            r.Responsable,
            r.Observaciones,
            r.ProximaDosis,
            e.Nombre          AS Estado,
            CASE
                WHEN r.EstadoID = 31        THEN N'Anulado'
                WHEN r.ProximaDosis IS NULL THEN N''
                WHEN r.ProximaDosis <  @Hoy THEN N'Vencida'
                ELSE N'Pendiente'
            END               AS EstadoProximaDosis
    FROM dbo.RegistrosSanitarios r
    JOIN dbo.Vacas v                   ON v.VacaID          = r.VacaID
    JOIN dbo.TiposRegistroSanitario t  ON t.TipoRegistroID  = r.TipoRegistroID
    JOIN dbo.Estados e                 ON e.EstadoID        = r.EstadoID
    WHERE r.VacaID = @VacaID
      AND v.UsuarioID = @UsuarioID
    ORDER BY r.FechaAplicacion DESC, r.RegistroID DESC;
END
GO

/* ============================================================
   TR1. trg_Vacas_Auditoria  (RN-5-CU12)
   Cada vez que se edita una vaca, guarda en AuditoriaVacas qué
   campo cambió, el valor anterior y el nuevo.
   ============================================================ */
CREATE OR ALTER TRIGGER dbo.trg_Vacas_Auditoria
ON dbo.Vacas
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditoriaVacas (VacaID, Campo, ValorAnterior, ValorNuevo)
    SELECT i.VacaID, c.Campo, c.Anterior, c.Nuevo
    FROM inserted i
    JOIN deleted  d ON d.VacaID = i.VacaID
    CROSS APPLY (VALUES
        (N'Raza',            d.Raza,                                   i.Raza),
        (N'Sexo',            d.Sexo,                                   i.Sexo),
        (N'FechaNacimiento', CONVERT(NVARCHAR(10), d.FechaNacimiento, 23), CONVERT(NVARCHAR(10), i.FechaNacimiento, 23)),
        (N'Peso',            CONVERT(NVARCHAR(20), d.Peso),            CONVERT(NVARCHAR(20), i.Peso)),
        (N'FotoUrl',         d.FotoUrl,                                i.FotoUrl),
        (N'Estado',          CONVERT(NVARCHAR(10), d.EstadoID),        CONVERT(NVARCHAR(10), i.EstadoID))
    ) AS c(Campo, Anterior, Nuevo)
    WHERE ISNULL(c.Anterior, N'') <> ISNULL(c.Nuevo, N'');
END
GO

/* ============================================================
   TR2. trg_Vacas_CancelarPublicaciones  (RN-4-CU13, RN-4-CU14)
   Si una vaca pasa de Viva (10) a Fallecida (11) o Vendida
   externamente (13), sus publicaciones Activas o Pausadas pasan a
   Cancelada (25) y se notifica al dueño (campanita, tipo 6).
   ============================================================ */
CREATE OR ALTER TRIGGER dbo.trg_Vacas_CancelarPublicaciones
ON dbo.Vacas
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT UPDATE(EstadoID) RETURN;

    DECLARE @Canceladas TABLE (PublicacionVacaID INT, UsuarioID INT, Identificacion NVARCHAR(50));

    UPDATE p
       SET p.EstadoID = 25,
           p.FechaActualizacion = SYSDATETIME()
    OUTPUT inserted.PublicacionVacaID, i.UsuarioID, i.Identificacion INTO @Canceladas
    FROM dbo.PublicacionesVaca p
    JOIN inserted i ON i.VacaID = p.VacaID
    JOIN deleted  d ON d.VacaID = i.VacaID
    WHERE d.EstadoID = 10
      AND i.EstadoID IN (11, 13)
      AND p.EstadoID IN (20, 21);

    -- Notificación al dueño (solo si existe la tabla del script 02)
    IF OBJECT_ID('dbo.Notificaciones') IS NOT NULL
    BEGIN
        INSERT INTO dbo.Notificaciones (UsuarioID, TipoNotificacionID, Mensaje, Enlace, FechaProgramada, EstadoID, FechaCreacion)
        SELECT c.UsuarioID, 6,
               N'Tu publicación de la vaca ' + c.Identificacion + N' se canceló automáticamente porque la vaca ya no está disponible.',
               N'/Publicaciones', SYSDATETIME(), 40, SYSDATETIME()
        FROM @Canceladas c;
    END
END
GO

/* ============================================================
   TR3. trg_RegistrosSanitarios_NoEliminar  (RN-5-CU07, RN-3-CU21)
   Un registro de vacuna/tratamiento nunca se borra de la base:
   en lugar de DELETE se marca como Anulado (31) y se deja la nota
   en Observaciones, para conservar la trazabilidad del historial.
   ============================================================ */
CREATE OR ALTER TRIGGER dbo.trg_RegistrosSanitarios_NoEliminar
ON dbo.RegistrosSanitarios
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE r
       SET r.EstadoID = 31,
           r.Observaciones = CONCAT(r.Observaciones,
                                    CASE WHEN r.Observaciones IS NULL THEN N'' ELSE N' | ' END,
                                    N'Anulado el ', CONVERT(NVARCHAR(16), SYSDATETIME(), 120),
                                    N' (se intentó eliminar)'),
           r.FechaActualizacion = SYSDATETIME()
    FROM dbo.RegistrosSanitarios r
    JOIN deleted d ON d.RegistroID = r.RegistroID;
END
GO

/* ============================================================
   PRUEBAS RÁPIDAS (quita los "--" y ejecútalas con datos)
   ============================================================ */
-- SP1: EXEC dbo.sp_BuscarPublicaciones @Orden = 'precio_asc';
-- SP2: EXEC dbo.sp_ReporteProduccion @UsuarioID = 1, @Desde = '2026-10-01', @Hasta = '2026-11-30';
-- SP3: EXEC dbo.sp_HistorialSanitario @VacaID = 1, @UsuarioID = 1;
-- TR1: UPDATE Vacas SET Peso = Peso + 10 WHERE VacaID = 1;  SELECT * FROM AuditoriaVacas;
-- TR2: (vaca con publicación activa) UPDATE Vacas SET EstadoID = 13 WHERE VacaID = 1;
--      SELECT EstadoID FROM PublicacionesVaca WHERE VacaID = 1;   -- debe quedar 25
-- TR3: DELETE FROM RegistrosSanitarios WHERE RegistroID = 1;
--      SELECT EstadoID, Observaciones FROM RegistrosSanitarios WHERE RegistroID = 1;  -- 31, sigue ahí

-- Lista de lo que quedó creado:
SELECT name AS Objeto, type_desc AS Tipo
FROM sys.objects
WHERE name IN ('sp_BuscarPublicaciones','sp_ReporteProduccion','sp_HistorialSanitario',
               'trg_Vacas_Auditoria','trg_Vacas_CancelarPublicaciones','trg_RegistrosSanitarios_NoEliminar',
               'AuditoriaVacas')
ORDER BY type_desc, name;
GO
