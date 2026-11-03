-- 1. Crear Base de Datos con el nombre entre corchetes
CREATE DATABASE [SistemaDeGestiónDB];
GO

USE [SistemaDeGestiónDB];
GO

-- 2. Tabla Rol
CREATE TABLE Rol (
    IdRol INT IDENTITY(1,1) PRIMARY KEY,
    NombreRol VARCHAR(30) NOT NULL UNIQUE
);

-- 3. Tabla Usuario
CREATE TABLE Usuario (
    IdUsuario INT IDENTITY(1,1) PRIMARY KEY,
    IdRol INT NOT NULL,
    Correo VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Activo BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Usuario_Rol FOREIGN KEY (IdRol) REFERENCES Rol(IdRol)
);

-- 4. Tabla Especialidad
CREATE TABLE Especialidad (
    IdEspecialidad INT IDENTITY(1,1) PRIMARY KEY,
    NombreEspecialidad VARCHAR(50) NOT NULL UNIQUE
);

-- 5. Tabla Consultorio
CREATE TABLE Consultorio (
    IdConsultorio INT IDENTITY(1,1) PRIMARY KEY,
    NombreConsultorio VARCHAR(50) NOT NULL UNIQUE
);

-- 6. Tabla Paciente
CREATE TABLE Paciente (
    IdPaciente INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL UNIQUE,
    DUI VARCHAR(10) NOT NULL UNIQUE,
    Nombre VARCHAR(50) NOT NULL,
    Apellido VARCHAR(50) NOT NULL,
    Telefono VARCHAR(15) NOT NULL,
    CONSTRAINT FK_Paciente_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario)
);

-- 7. Tabla Medico
CREATE TABLE Medico (
    IdMedico INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL UNIQUE,
    IdEspecialidad INT NOT NULL,
    IdConsultorio INT NOT NULL,
    Nombre VARCHAR(50) NOT NULL,
    Apellido VARCHAR(50) NOT NULL,
    DuracionConsultaMin INT NOT NULL DEFAULT 20,
    CONSTRAINT FK_Medico_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario),
    CONSTRAINT FK_Medico_Especialidad FOREIGN KEY (IdEspecialidad) REFERENCES Especialidad(IdEspecialidad),
    CONSTRAINT FK_Medico_Consultorio FOREIGN KEY (IdConsultorio) REFERENCES Consultorio(IdConsultorio)
);

-- 8. Tabla HorarioMedico
CREATE TABLE HorarioMedico (
    IdHorario INT IDENTITY(1,1) PRIMARY KEY,
    IdMedico INT NOT NULL,
    DiaSemana INT NOT NULL CHECK (DiaSemana BETWEEN 1 AND 7),
    HoraInicio TIME NOT NULL,
    HoraFin TIME NOT NULL,
    CONSTRAINT FK_HorarioMedico_Medico FOREIGN KEY (IdMedico) REFERENCES Medico(IdMedico)
);

-- 9. Tabla EstadoCita
CREATE TABLE EstadoCita (
    IdEstadoCita INT IDENTITY(1,1) PRIMARY KEY,
    NombreEstado VARCHAR(30) NOT NULL UNIQUE
);

-- 10. Tabla Cita
CREATE TABLE Cita (
    IdCita INT IDENTITY(1,1) PRIMARY KEY,
    CodigoTurno VARCHAR(10) NOT NULL,
    IdPaciente INT NOT NULL,
    IdMedico INT NOT NULL,
    IdEstadoCita INT NOT NULL,
    FechaHora DATETIME NOT NULL,
    MotivoConsulta VARCHAR(255) NULL,
    HoraCheckIn DATETIME NULL,
    CONSTRAINT FK_Cita_Paciente FOREIGN KEY (IdPaciente) REFERENCES Paciente(IdPaciente),
    CONSTRAINT FK_Cita_Medico FOREIGN KEY (IdMedico) REFERENCES Medico(IdMedico),
    CONSTRAINT FK_Cita_EstadoCita FOREIGN KEY (IdEstadoCita) REFERENCES EstadoCita(IdEstadoCita)
);

-- 11. Tabla ListaEspera
CREATE TABLE ListaEspera (
    IdListaEspera INT IDENTITY(1,1) PRIMARY KEY,
    IdPaciente INT NOT NULL,
    IdMedico INT NULL,
    IdEspecialidad INT NOT NULL,
    FechaDeseada DATE NOT NULL,
    Prioridad INT NOT NULL DEFAULT 2,
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE(),
    Atendido BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ListaEspera_Paciente FOREIGN KEY (IdPaciente) REFERENCES Paciente(IdPaciente),
    CONSTRAINT FK_ListaEspera_Medico FOREIGN KEY (IdMedico) REFERENCES Medico(IdMedico),
    CONSTRAINT FK_ListaEspera_Especialidad FOREIGN KEY (IdEspecialidad) REFERENCES Especialidad(IdEspecialidad)
);

-- 12. Tabla RegistroAtencion
CREATE TABLE RegistroAtencion (
    IdRegistro INT IDENTITY(1,1) PRIMARY KEY,
    IdCita INT NOT NULL UNIQUE,
    NotasConsulta VARCHAR(MAX) NULL,
    Diagnostico VARCHAR(MAX) NULL,
    Indicaciones VARCHAR(MAX) NULL,
    CONSTRAINT FK_RegistroAtencion_Cita FOREIGN KEY (IdCita) REFERENCES Cita(IdCita)
);

-- 13. Tabla Expediente
CREATE TABLE Expediente (
    IdExpediente INT IDENTITY(1,1) PRIMARY KEY,
    IdPaciente INT NOT NULL UNIQUE,
    FechaApertura DATETIME NOT NULL DEFAULT GETDATE(),
    GrupoSanguineo VARCHAR(5) NULL,
    Alergias VARCHAR(255) NULL,
    Antecedentes VARCHAR(MAX) NULL,
    CONSTRAINT FK_Expediente_Paciente FOREIGN KEY (IdPaciente) REFERENCES Paciente(IdPaciente)
);

-- Población básica de datos iniciales
INSERT INTO Rol (NombreRol) VALUES ('Administrador'), ('Recepcionista'), ('Medico'), ('Paciente');
INSERT INTO EstadoCita (NombreEstado) VALUES ('Agendada'), ('En Espera'), ('En Consulta'), ('Finalizada'), ('Cancelada'), ('No-Show');

SELECT * FROM ListaEspera
SELECT * FROM Medico
SELECT * FROM Rol

USE [SistemaDeGestiónDB];
GO

-- 1. TABLAS BASE
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Rol' AND xtype='U')
BEGIN
    CREATE TABLE Rol (
        IdRol INT IDENTITY(1,1) PRIMARY KEY,
        NombreRol VARCHAR(30) NOT NULL UNIQUE
    );
END

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EstadoCita' AND xtype='U')
BEGIN
    CREATE TABLE EstadoCita (
        IdEstadoCita INT IDENTITY(1,1) PRIMARY KEY,
        NombreEstado VARCHAR(30) NOT NULL UNIQUE
    );
END

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Especialidad' AND xtype='U')
BEGIN
    CREATE TABLE Especialidad (
        IdEspecialidad INT IDENTITY(1,1) PRIMARY KEY,
        NombreEspecialidad VARCHAR(50) NOT NULL UNIQUE
    );
END

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Consultorio' AND xtype='U')
BEGIN
    CREATE TABLE Consultorio (
        IdConsultorio INT IDENTITY(1,1) PRIMARY KEY,
        NombreConsultorio VARCHAR(50) NOT NULL UNIQUE
    );
END

-- 2. TABLA USUARIO
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Usuario' AND xtype='U')
BEGIN
    CREATE TABLE Usuario (
        IdUsuario INT IDENTITY(1,1) PRIMARY KEY,
        IdRol INT NOT NULL,
        Correo VARCHAR(100) NOT NULL UNIQUE,
        PasswordHash VARCHAR(255) NOT NULL,
        Activo BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_Usuario_Rol FOREIGN KEY (IdRol) REFERENCES Rol(IdRol)
    );
END

-- 3. PERFILES: MEDICO Y PACIENTE
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Medico' AND xtype='U')
BEGIN
    CREATE TABLE Medico (
        IdMedico INT IDENTITY(1,1) PRIMARY KEY,
        IdUsuario INT NOT NULL UNIQUE,
        IdEspecialidad INT NOT NULL,
        IdConsultorio INT NOT NULL,
        Nombre VARCHAR(50) NOT NULL,
        Apellido VARCHAR(50) NOT NULL,
        DuracionConsultaMin INT NOT NULL DEFAULT 20,
        CONSTRAINT FK_Medico_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario),
        CONSTRAINT FK_Medico_Especialidad FOREIGN KEY (IdEspecialidad) REFERENCES Especialidad(IdEspecialidad),
        CONSTRAINT FK_Medico_Consultorio FOREIGN KEY (IdConsultorio) REFERENCES Consultorio(IdConsultorio)
    );
END

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Paciente' AND xtype='U')
BEGIN
    CREATE TABLE Paciente (
        IdPaciente INT IDENTITY(1,1) PRIMARY KEY,
        IdUsuario INT NOT NULL UNIQUE,
        DUI VARCHAR(10) NOT NULL UNIQUE,
        Nombre VARCHAR(50) NOT NULL,
        Apellido VARCHAR(50) NOT NULL,
        Telefono VARCHAR(15) NOT NULL,
        CONSTRAINT FK_Paciente_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario)
    );
END

-- 4. TABLAS DE CITAS Y EXPEDIENTES (ON DELETE NO ACTION para evitar el error de cascada)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Cita' AND xtype='U')
BEGIN
    CREATE TABLE Cita (
        IdCita INT IDENTITY(1,1) PRIMARY KEY,
        CodigoTurno VARCHAR(10) NOT NULL,
        IdPaciente INT NOT NULL,
        IdMedico INT NOT NULL,
        IdEstadoCita INT NOT NULL,
        FechaHora DATETIME NOT NULL,
        MotivoConsulta VARCHAR(255) NULL,
        HoraCheckIn DATETIME NULL,
        CONSTRAINT FK_Cita_Paciente FOREIGN KEY (IdPaciente) REFERENCES Paciente(IdPaciente) ON DELETE NO ACTION,
        CONSTRAINT FK_Cita_Medico FOREIGN KEY (IdMedico) REFERENCES Medico(IdMedico) ON DELETE NO ACTION,
        CONSTRAINT FK_Cita_EstadoCita FOREIGN KEY (IdEstadoCita) REFERENCES EstadoCita(IdEstadoCita)
    );
END

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Expediente' AND xtype='U')
BEGIN
    CREATE TABLE Expediente (
        IdExpediente INT IDENTITY(1,1) PRIMARY KEY,
        IdPaciente INT NOT NULL UNIQUE,
        FechaApertura DATETIME NOT NULL DEFAULT GETDATE(),
        GrupoSanguineo VARCHAR(5) NULL,
        Alergias VARCHAR(255) NULL,
        Antecedentes VARCHAR(MAX) NULL,
        CONSTRAINT FK_Expediente_Paciente FOREIGN KEY (IdPaciente) REFERENCES Paciente(IdPaciente)
    );
END
GO

-- =================================================================
-- POBLACIÓN DE DATOS DE PRUEBA
-- =================================================================

-- Roles
IF NOT EXISTS (SELECT * FROM Rol)
BEGIN
    INSERT INTO Rol (NombreRol) VALUES 
    ('Administrador'), 
    ('Recepcionista'), 
    ('Medico'), 
    ('Paciente');
END

-- Estados de Cita
IF NOT EXISTS (SELECT * FROM EstadoCita)
BEGIN
    INSERT INTO EstadoCita (NombreEstado) VALUES 
    ('Agendada'), 
    ('En Espera'), 
    ('En Consulta'), 
    ('Finalizada'), 
    ('Cancelada');
END

-- Especialidades y Consultorios
IF NOT EXISTS (SELECT * FROM Especialidad)
BEGIN
    INSERT INTO Especialidad (NombreEspecialidad) VALUES ('Medicina General');
END

IF NOT EXISTS (SELECT * FROM Consultorio)
BEGIN
    INSERT INTO Consultorio (NombreConsultorio) VALUES ('Consultorio 101');
END

-- Usuarios de Prueba (Contraseña: 123456)
IF NOT EXISTS (SELECT * FROM Usuario)
BEGIN
    DECLARE @idAdmin INT = (SELECT IdRol FROM Rol WHERE NombreRol = 'Administrador');
    DECLARE @idRecep INT = (SELECT IdRol FROM Rol WHERE NombreRol = 'Recepcionista');
    DECLARE @idMedico INT = (SELECT IdRol FROM Rol WHERE NombreRol = 'Medico');
    DECLARE @idPaciente INT = (SELECT IdRol FROM Rol WHERE NombreRol = 'Paciente');

    INSERT INTO Usuario (IdRol, Correo, PasswordHash, Activo) VALUES 
    (@idAdmin, 'admin@clinica.com', '123456', 1),
    (@idRecep, 'recepcion@clinica.com', '123456', 1),
    (@idMedico, 'medico1@clinica.com', '123456', 1),
    (@idPaciente, 'paciente1@clinica.com', '123456', 1);

    -- Perfil de Médico
    DECLARE @idUserMed INT = (SELECT IdUsuario FROM Usuario WHERE Correo = 'medico1@clinica.com');
    DECLARE @idEsp INT = (SELECT IdEspecialidad FROM Especialidad WHERE NombreEspecialidad = 'Medicina General');
    DECLARE @idCons INT = (SELECT IdConsultorio FROM Consultorio WHERE NombreConsultorio = 'Consultorio 101');

    INSERT INTO Medico (IdUsuario, IdEspecialidad, IdConsultorio, Nombre, Apellido, DuracionConsultaMin)
    VALUES (@idUserMed, @idEsp, @idCons, 'Carlos', 'Gómez', 20);

    -- Perfil de Paciente
    DECLARE @idUserPac INT = (SELECT IdUsuario FROM Usuario WHERE Correo = 'paciente1@clinica.com');

    INSERT INTO Paciente (IdUsuario, DUI, Nombre, Apellido, Telefono)
    VALUES (@idUserPac, '01234567-8', 'Juan', 'Pérez', '7777-8888');

    -- Cita de Prueba
    DECLARE @idPac INT = (SELECT IdPaciente FROM Paciente WHERE DUI = '01234567-8');
    DECLARE @idMed INT = (SELECT IdMedico FROM Medico WHERE Nombre = 'Carlos');
    DECLARE @idEstado INT = (SELECT IdEstadoCita FROM EstadoCita WHERE NombreEstado = 'En Espera');

    INSERT INTO Cita (CodigoTurno, IdPaciente, IdMedico, IdEstadoCita, FechaHora, MotivoConsulta, HoraCheckIn)
    VALUES ('T-001', @idPac, @idMed, @idEstado, GETDATE(), 'Consulta de prueba por dolor de cabeza', GETDATE());
END
GO

select * from Rol
select * from Cita
select * from Consultorio
select * from Especialidad
select * from EstadoCita
select * from Expediente
select * from HorarioMedico
select * from ListaEspera
select * from Medico
select * from Paciente
select * from RegistroAtencion
select * from Usuario

