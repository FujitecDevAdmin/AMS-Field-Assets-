import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';
import 'audit_models.dart';

class AuditsApi {
  const AuditsApi(this._dio);
  final Dio _dio;

  Future<List<AssignedAudit>> listMine() async {
    Response<Map<String, dynamic>> response;
    try {
      response = await _dio.get<Map<String, dynamic>>(
        '/api/v1/verification/my-audits',
      );
    } on DioException catch (error) {
      if (error.type == DioExceptionType.connectionError ||
          error.type == DioExceptionType.connectionTimeout ||
          error.type == DioExceptionType.receiveTimeout) {
        throw Exception(
          'The audit server could not be reached. Check that the API is running on port 5069 and try again.',
        );
      }
      throw Exception('Assigned audits could not be refreshed.');
    }
    if (response.statusCode != 200 || response.data == null) {
      throw Exception('Assigned audits could not be loaded.');
    }
    return (response.data!['rows'] as List<dynamic>)
        .map((row) => AssignedAudit.fromJson(row as Map<String, dynamic>))
        .toList();
  }

  Future<AssignedAsset> resolveScan({
    required AssignedAudit audit,
    required String scannedValue,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '/api/v1/verification/my-audits/${audit.id}/resolve-scan',
      data: <String, dynamic>{'scanCode': scannedValue},
    );
    if (response.statusCode != 200 || response.data == null) {
      final detail = response.data?['detail'] as String?;
      throw Exception(
        detail ?? 'This asset is not assigned to this audit and branch.',
      );
    }
    return AssignedAsset.fromJson(response.data!);
  }

  Future<void> verify({
    required AssignedAudit audit,
    required AssignedAsset asset,
    required String workingCondition,
    required bool serialVerified,
    required String? remarks,
  }) async {
    final persistedIdentifier = asset.qrCodeValue?.trim().isNotEmpty == true
        ? asset.qrCodeValue!.trim()
        : asset.barcodeValue?.trim().isNotEmpty == true
        ? asset.barcodeValue!.trim()
        : asset.assetNumber;
    final response = await _dio.post<Map<String, dynamic>>(
      '/api/v1/verification/verifications',
      data: <String, dynamic>{
        'cycleId': audit.id,
        'assetId': asset.id,
        'isBulkCount': false,
        // The resolve-scan endpoint has already validated the raw scanner
        // payload. Save the configured identifier, not a long encoded URL.
        'scannedQrValue': persistedIdentifier,
        'workingCondition': workingCondition,
        'serialVerified': serialVerified,
        'locationId': audit.branchId,
        'remarks': remarks,
      },
    );
    if ((response.statusCode ?? 500) >= 300) {
      final detail = response.data?['detail'] as String?;
      throw Exception(detail ?? 'Verification could not be saved.');
    }
  }
}

final auditsApiProvider = Provider<AuditsApi>(
  (ref) => AuditsApi(ref.watch(dioProvider)),
);
