-- =============================================
-- Author:		Meylin Vallecillos
-- Create date: 30/08/2016
-- Description:	Obtiene email para envio de reportes
-- =============================================
CREATE PROCEDURE [dbo].[Sp_DbDatos_Arrendamiento_Pos_Alert_Destination_Get]
	-- Add the parameters for the stored procedure here
	@IdSistema bigint
	, @IdTipo bigint
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	BEGIN TRY  
	 INSERT INTO [Sistemas].[Sistemas.Verify_Procedures_Use]
       ([Name])
	VALUES
       (OBJECT_NAME(@@PROCID))
	END TRY  
	BEGIN CATCH  
	    
	END CATCH;  

    -- Insert statements for procedure here
	SELECT
		emp.Email_Empresa
	FROM 
				   [dbdatos].[Mensajeria].[DbDatos_Email_Destinations] as ed 
		inner join [dbdatos].[Empleados].DbDatos_Empleados as emp on ed.IdEmpleado_Fk = emp.Id
	where 
		ed.Estado = 1
		and emp.Estado = 1
		and IdSistema_Fk = @IdSistema
		and ed.IdTipo_Fk = @IdTipo
END 