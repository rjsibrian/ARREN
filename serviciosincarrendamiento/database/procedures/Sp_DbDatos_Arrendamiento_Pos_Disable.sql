-- =============================================
-- Author:		Meylin Vallecillos
-- Create date: 01/08/2016
-- Description:	Desactivar comercios de arrendamieto Pos
-- =============================================
CREATE PROCEDURE [dbo].[Sp_DbDatos_Arrendamiento_Pos_Disable]
	-- Add the parameters for the stored procedure here

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here

	--cambiar estado a los que se les cambio monto en DftMan o se desactivaron en Tandem
	BEGIN TRY  
	 INSERT INTO [Sistemas].[Sistemas.Verify_Procedures_Use]
       ([Name])
	VALUES
       (OBJECT_NAME(@@PROCID))
	END TRY  
	BEGIN CATCH  
	    
	END CATCH;  


	UPDATE c
	SET 
		c.Fecha_Desabilitado = GETDATE(),
		c.Descripcion_Desa = 'DEBITO SUSPENDIDO POR ADMINISTRATIVOS',
		c.Estado = 0
	FROM [dbdatos].[dbo].[DBDATOS_ARRENDAMIENTO_COMERCIOS_TEMP] as c 
	where 
		(convert(date, Fecha_Modificacion ) < CONVERT(date, getdate()) or Fecha_Modificacion is null and  convert(date, Fecha_Creacion ) < CONVERT(date, getdate())) 
		and Estado > 0 

	--Cambiar estado a los que llevan mas de 3 alertas 
	--UPDATE c
	--SET 
	--	c.Fecha_Desabilitado = GETDATE(),
	--	c.Descripcion_Desa = 'DEBITO SUSPENDIDO POR FALTA DE TRANSACCIONES',
	--	c.Estado = 0
	--from dbdatos.dbo.DBDATOS_ARRENDAMIENTO_COMERCIOS_TEMP  as c
	--inner join dbdatos.dbo.Vw_Arrendamiento_Pos_comercios as Vc on c.Retailer = vc.retailer
	--WHERE 
	--	c.Estado > 0
	--	and c.Alertas > 3
END 