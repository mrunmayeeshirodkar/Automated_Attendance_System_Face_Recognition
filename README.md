# Automated Attendance Management System using Face Detection and Recognition

An Attendance Management System based on auto switch-on and shut-sown facility. It has abilities to capture images, detects and recognizes faces, marks attendance. 
Face Detection is implemented using integral image, Haar-like Features, AdaBoost and Cascading Classifier(Viola-Jones Algorithm). Face Recognition is implemented using Local Binary Pattern Histogram. 
This has functionalities such as getting headcount based on the lecture schedule, generating reports at the end of the month, options for editing attendance for individual student.

This system need 15 - 20 photos of each person with different lighting, face angles, saturation for training the system. Based on this training it gives correct label to the person in the image captured. This system can be further improvised to capture photos from live feed and make it completely automated.
