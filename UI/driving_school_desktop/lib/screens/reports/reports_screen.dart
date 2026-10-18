import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';
import 'lesson_status_report_tab.dart';
import 'vehicle_service_report_tab.dart';

/// 4.1.4 - two cards side by side on a wide window, stacked on a narrow one.
class ReportsScreen extends StatelessWidget {
  const ReportsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: AppSpacing.page,
      child: LayoutBuilder(
        builder: (context, constraints) {
          if (constraints.maxWidth < 900) {
            return const SingleChildScrollView(
              child: Column(
                children: [
                  LessonStatusReportTab(),
                  SizedBox(height: AppSpacing.lg),
                  VehicleServiceReportTab(),
                ],
              ),
            );
          }

          return const Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(child: LessonStatusReportTab()),
              SizedBox(width: AppSpacing.lg),
              Expanded(child: VehicleServiceReportTab()),
            ],
          );
        },
      ),
    );
  }
}
