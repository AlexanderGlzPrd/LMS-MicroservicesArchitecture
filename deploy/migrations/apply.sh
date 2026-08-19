#!/bin/sh
set -e

: "${COURSE_AUTHORING_CONNECTION:?falta COURSE_AUTHORING_CONNECTION}"
: "${ENROLLMENT_CONNECTION:?falta ENROLLMENT_CONNECTION}"
: "${LEARNING_CONNECTION:?falta LEARNING_CONNECTION}"
: "${CERTIFICATION_CONNECTION:?falta CERTIFICATION_CONNECTION}"
: "${PURCHASE_CONNECTION:?falta PURCHASE_CONNECTION}"
: "${PAYMENTS_CONNECTION:?falta PAYMENTS_CONNECTION}"

apply() {
    echo "==> $1"
    /bundles/"$1" --connection "$2"
}

apply course-authoring     "$COURSE_AUTHORING_CONNECTION"
apply enrollment           "$ENROLLMENT_CONNECTION"
apply learning             "$LEARNING_CONNECTION"
apply certification        "$CERTIFICATION_CONNECTION"
apply paid-enrollment      "$PURCHASE_CONNECTION"
apply payment-provider-sim "$PAYMENTS_CONNECTION"