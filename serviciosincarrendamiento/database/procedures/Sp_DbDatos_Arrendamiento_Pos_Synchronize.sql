-- =============================================
-- Author:		MEYLIN VALLECILLOS
-- Create date: 13/04/2016
-- Description:	SINCRONIZAR COMERCIOS CON DEUDA
-- =============================================
CREATE PROCEDURE [dbo].[Sp_DbDatos_Arrendamiento_Pos_Synchronize]
	-- Add the parameters for the stored procedure here
	@RETAILER AS VARCHAR(19),
	@RTLPADRE AS VARCHAR(19),
	@CONSOLIDAR AS INT,
	@ARRENDAMIENTO AS DECIMAL(18,2),
	@NOPOS AS INT,
	@USER AS VARCHAR(25)
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
	DECLARE @VALIDAR AS INT
	SET @VALIDAR = (SELECT COUNT(*) FROM dbdatos.dbo.DBDATOS_ARRENDAMIENTO_COMERCIOS_TEMP WHERE RETAILER=@RETAILER)

	IF (@VALIDAR = 0)
		BEGIN
			INSERT INTO dbdatos.dbo.DBDATOS_ARRENDAMIENTO_COMERCIOS_TEMP 
				(RETAILER, RTLPADRE, CONSOLIDAR, MTO_ARRENDAMIENTO, NO_POS, USUARIO_CREACION)
				VALUES(@RETAILER, @RTLPADRE, @CONSOLIDAR, @ARRENDAMIENTO, @NOPOS, @USER)
		END 
	ELSE 
		BEGIN
			UPDATE dbdatos.dbo.DBDATOS_ARRENDAMIENTO_COMERCIOS_TEMP 
				SET 
					  MTO_ARRENDAMIENTO = @ARRENDAMIENTO
					, RTLPADRE = @RTLPADRE
					, CONSOLIDAR = @CONSOLIDAR
					, NO_POS = @NOPOS
					, USUARIO_MODIFICACION = @USER
					, FECHA_MODIFICACION = GETDATE()
				WHERE 
					RETAILER = @RETAILER
					and ESTADO > 0					
		END
END 