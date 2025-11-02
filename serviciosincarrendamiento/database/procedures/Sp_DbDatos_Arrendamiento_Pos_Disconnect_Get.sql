-- =============================================
-- Author:		Meylin Arely
-- Create date: 26/08/2016
-- Description: Devuelve informacion de desactivados
-- =============================================
CREATE PROCEDURE [dbo].[Sp_DbDatos_Arrendamiento_Pos_Disconnect_Get]
	-- Add the parameters for the stored procedure here
	@Estado bit
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
		 pc.[retailer]
		,pc.[monto]
		,pc.[saldo]
		,pc.[debe]
		,pc.[inicio]
		,pc.[retiro]
		,pc.[pos]
		,case when (pc.[estado] =1) then 'Activo' else 'Inactivo' end as estado
		,b.Nombre [nombre]
		,i.Nombre [banco]
	FROM 
		[dbdatos].[dbo].[Vw_Arrendamiento_Pos_comercios] pc
		LEFT  JOIN [dbdatos].[Comercios].[DbDatos_Bussiness_Data] b on pc.Retailer = b.Retailer
		LEFT  JOIN [dbdatos].[Comercios].[DbDatos_Afiliation_Data] a on b.IdAfiliacion_Fk = a.Id
		LEFT  JOIN [dbdatos].[Instituciones].[DbDatos_Instituciones] i on a.IdInstitucion_Fk = i.Id 
	WHERE 
		pc.[estado] = 0
	ORDER BY 
		pc.[debe]
END 